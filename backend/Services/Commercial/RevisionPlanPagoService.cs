using System.Data;
using System.Security.Claims;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace BudgetControl.Api.Services.Commercial;

public sealed class RevisionPlanPagoService : IRevisionPlanPagoService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public RevisionPlanPagoService(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    private (string Id, string Name) Authorize()
    {
        var user = _http.HttpContext?.User;
        var id = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user?.FindFirstValue(ClaimTypes.Name);
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("Admin") ||
            string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || id.Length > 100 || name.Length > 100)
            throw new UnauthorizedAccessException();
        return (id, name);
    }

    public async Task<PreparacionRevisionPlanResponse> PrepararAsync(int acuerdoId, int viaId, int planId, CancellationToken ct = default)
    {
        Authorize();
        // A consistent read snapshot; confirmation always reloads under row locks.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var plan = await _db.PlanesPago.AsNoTracking().Include(p => p.AcuerdoComercialVia).ThenInclude(v => v.AcuerdoComercial)
            .Include(p => p.Cuotas).SingleOrDefaultAsync(p => p.Id == planId && p.AcuerdoComercialViaId == viaId, ct)
            ?? throw new KeyNotFoundException("Plan no encontrado para la vía indicada.");
        ValidatePlan(plan, acuerdoId, planId);
        var result = await Prepare(plan, ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<ConfirmacionRevisionPlanResponse> ConfirmarAsync(RevisionPlanPagoRequest request, CancellationToken ct = default)
    {
        var user = Authorize();
        ValidateRequest(request);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            // Transaction-local: no connection/pool setting leaks. All waits are bounded.
            await _db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);
            if (!await _db.PlanesPago.AsNoTracking().AnyAsync(p => p.Id == request.PlanPagoId &&
                p.AcuerdoComercialViaId == request.AcuerdoComercialViaId &&
                p.AcuerdoComercialVia.AcuerdoComercialId == request.AcuerdoComercialId, ct))
                throw new KeyNotFoundException("El acuerdo, la vía y el plan no coinciden.");
            var plan = await PlanRevisionLock.AcquireAsync(_db, request.AcuerdoComercialViaId, ct);
            await _db.Entry(plan.AcuerdoComercialVia).Reference(v => v.AcuerdoComercial).LoadAsync(ct);

            // Read again AFTER the aggregate lock: concurrent same-key retries see the committed audit.
            var prior = await _db.RevisionesPlanesPago.AsNoTracking().Include(r => r.Detalles)
                .SingleOrDefaultAsync(r => r.SolicitudId == request.SolicitudId, ct);
            if (prior != null)
            {
                if (prior.PlanPagoId != request.PlanPagoId || prior.AcuerdoComercialId != request.AcuerdoComercialId ||
                    prior.AcuerdoComercialViaId != request.AcuerdoComercialViaId || prior.UsuarioId != user.Id)
                    throw Conflict("El identificador de solicitud ya pertenece a otra operación o usuario.");
                var replay = new ConfirmacionRevisionPlanResponse { SolicitudYaProcesada = true, Revision = Map(prior), EstadoActual = await Prepare(plan, ct) };
                await tx.CommitAsync(ct);
                return replay;
            }
            ValidatePlan(plan, request.AcuerdoComercialId, request.PlanPagoId);
            ValidateVersions(plan, request);
            var before = await Prepare(plan, ct);
            var history = before.Cuotas.ToDictionary(c => c.Id);
            var revision = new RevisionPlanPago
            {
                SolicitudId = request.SolicitudId, AcuerdoComercialId = request.AcuerdoComercialId,
                AcuerdoComercialViaId = plan.AcuerdoComercialViaId, PlanPagoId = plan.Id,
                MontoAnterior = plan.AcuerdoComercialVia.MontoActual, MontoNuevo = request.NuevoMontoActual,
                Diferencia = request.NuevoMontoActual - plan.AcuerdoComercialVia.MontoActual,
                Comentario = request.Comentario.Trim(), UsuarioId = user.Id, Usuario = user.Name,
                Fecha = DateTime.UtcNow, MonedaCodigo = plan.AcuerdoComercialVia.MonedaCodigo
            };
            var byId = plan.Cuotas.ToDictionary(c => c.Id);
            var removed = new HashSet<int>();
            var added = new List<(CuotaComercial Cuota, RevisionPlanPagoDetalle Detail)>();

            // Validate all existing changes, including explicit versions, before applying any.
            foreach (var change in request.Cambios.Where(c => c.Operacion != CambioCuotaRevision.Agregar))
            {
                if (!byId.TryGetValue(change.CuotaId!.Value, out var cuota)) throw Conflict("La cuota no pertenece al plan vigente.");
                if (cuota.Version != change.Version) throw Conflict("La cuota cambió. Recargue la revisión.");
                var reason = history[cuota.Id].MotivoBloqueoModificacion;
                if (reason != null) throw Conflict($"Cuota {cuota.NumeroCuota}: {reason}");
            }
            foreach (var change in request.Cambios.Where(c => c.Operacion == CambioCuotaRevision.Modificar))
            {
                var cuota = byId[change.CuotaId!.Value];
                var detail = Snapshot(cuota, OperacionRevisionPlanPago.Modificada);
                if (change.Importe.HasValue) cuota.ImporteOriginal = change.Importe.Value;
                if (change.Vencimiento.HasValue) cuota.FechaVencimiento = CommercialCalendar.Normalize(change.Vencimiento.Value);
                cuota.SaldoPendiente = cuota.ImporteOriginal;
                cuota.Estado = CommercialCalendar.IsOverdue(cuota.FechaVencimiento) ? CuotaEstado.Vencida : CuotaEstado.Pendiente;
                Complete(detail, cuota);
                revision.Detalles.Add(detail);
            }
            foreach (var change in request.Cambios.Where(c => c.Operacion == CambioCuotaRevision.Retirar)
                .OrderByDescending(c => byId[c.CuotaId!.Value].NumeroCuota))
            {
                var cuota = byId[change.CuotaId!.Value];
                if (cuota.TipoCuota == TipoCuota.Cuota && Last(plan, removed)?.Id != cuota.Id)
                    throw Conflict($"La cuota {cuota.NumeroCuota} no es la última cuota ordinaria activa. No se pueden saltar cuotas.");
                var hasHistory = history[cuota.Id].TieneHistoria;
                var detail = Snapshot(cuota, hasHistory ? OperacionRevisionPlanPago.Excluida : OperacionRevisionPlanPago.Eliminada);
                if (hasHistory)
                {
                    cuota.Estado = CuotaEstado.Anulada;
                    cuota.SaldoPendiente = 0;
                    Complete(detail, cuota);
                }
                else
                {
                    detail.CuotaComercialId = null;
                    _db.CuotasComerciales.Remove(cuota);
                }
                removed.Add(cuota.Id);
                revision.Detalles.Add(detail);
            }
            var next = before.SiguienteNumeroCuota;
            foreach (var change in request.Cambios.Where(c => c.Operacion == CambioCuotaRevision.Agregar).OrderBy(c => c.Numero))
            {
                if (change.Numero != next) throw new InvalidOperationException($"La siguiente cuota ordinaria debe tener número {next}; no se reutilizan números históricos.");
                next = checked(next + 1);
                var cuota = new CuotaComercial
                {
                    PlanPagoId = plan.Id, NumeroCuota = change.Numero!.Value, TipoCuota = TipoCuota.Cuota,
                    ImporteOriginal = change.Importe!.Value, SaldoPendiente = change.Importe.Value,
                    FechaVencimiento = CommercialCalendar.Normalize(change.Vencimiento!.Value),
                    Estado = CommercialCalendar.IsOverdue(change.Vencimiento.Value) ? CuotaEstado.Vencida : CuotaEstado.Pendiente
                };
                plan.Cuotas.Add(cuota);
                var detail = new RevisionPlanPagoDetalle { PlanPagoId = plan.Id, NumeroCuota = cuota.NumeroCuota,
                    TipoCuota = cuota.TipoCuota, Operacion = OperacionRevisionPlanPago.Agregada };
                Complete(detail, cuota);
                added.Add((cuota, detail));
            }
            var active = plan.Cuotas.Where(c => !removed.Contains(c.Id) && c.Estado != CuotaEstado.Anulada).ToList();
            if (active.Any(c => c.ImporteOriginal < 0) || active.Sum(c => c.ImporteOriginal) != request.NuevoMontoActual)
                throw new InvalidOperationException("El total vigente de las obligaciones no coincide exactamente con el nuevo MontoActual.");
            plan.AcuerdoComercialVia.MontoActual = request.NuevoMontoActual;
            plan.CantidadCuotas = active.Count(c => c.TipoCuota == TipoCuota.Cuota);
            plan.TieneAnticipo = active.Any(c => c.TipoCuota == TipoCuota.Anticipo);
            plan.MontoAnticipo = active.Where(c => c.TipoCuota == TipoCuota.Anticipo).Sum(c => c.ImporteOriginal);
            var first = active.Where(c => c.TipoCuota == TipoCuota.Cuota).OrderBy(c => c.NumeroCuota).FirstOrDefault();
            if (first != null) plan.FechaPrimerVencimiento = first.FechaVencimiento;
            // Even a date-only revision changes the aggregate token; the new value is returned by xmin.
            _db.Entry(plan).Property(p => p.CantidadCuotas).IsModified = true;
            await _db.SaveChangesAsync(ct); // allocate new quota IDs inside the SAME transaction
            foreach (var (cuota, detail) in added)
            {
                detail.CuotaComercialId = cuota.Id; detail.CuotaOriginalId = cuota.Id;
                revision.Detalles.Add(detail);
            }
            revision.TipoRevision = request.Cambios.Select(c => c.Operacion).Distinct().Count() > 1 ? TipoRevisionPlanPago.Mixta :
                revision.Diferencia < 0 ? TipoRevisionPlanPago.Reduccion : revision.Diferencia > 0 ? TipoRevisionPlanPago.Ampliacion : TipoRevisionPlanPago.Modificacion;
            _db.RevisionesPlanesPago.Add(revision);
            await _db.SaveChangesAsync(ct);
            foreach (var id in removed.Where(id => !history[id].TieneHistoria)) plan.Cuotas.Remove(byId[id]);
            var result = new ConfirmacionRevisionPlanResponse { Revision = Map(revision), EstadoActual = await Prepare(plan, ct) };
            await tx.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (IsConcurrent(ex))
        {
            _db.ChangeTracker.Clear();
            throw new RevisionPlanConflictException("El plan o sus movimientos cambiaron, o están siendo utilizados. Recargue antes de confirmar nuevamente.", ex);
        }
        catch { _db.ChangeTracker.Clear(); throw; }
    }

    private static bool IsConcurrent(Exception ex) => ex is DbUpdateConcurrencyException || ex is TimeoutException ||
        ex is PostgresException { SqlState: "55P03" or "40P01" or "40001" or "23505" or "23503" or "23001" } ||
        ex.InnerException != null && IsConcurrent(ex.InnerException);
    private static RevisionPlanConflictException Conflict(string message) => new(message);

    private static void ValidatePlan(PlanPago plan, int acuerdoId, int planId)
    {
        var via = plan.AcuerdoComercialVia;
        if (plan.Id != planId || via.AcuerdoComercialId != acuerdoId) throw Conflict("Cambió la pertenencia del plan.");
        if (via.Estado is AcuerdoEstado.Anulado or AcuerdoEstado.Finalizado || via.AcuerdoComercial.Estado is AcuerdoEstado.Anulado or AcuerdoEstado.Finalizado ||
            (via.Estado != AcuerdoEstado.Aprobado && via.AcuerdoComercial.Estado != AcuerdoEstado.Aprobado))
            throw Conflict("La revisión requiere una vía o acuerdo aprobado y vigente.");
        if (via.ModalidadCobro != ModalidadCobro.Planificada) throw new InvalidOperationException("La vía no utiliza un plan de pago.");
        if (string.IsNullOrWhiteSpace(via.MonedaCodigo)) throw new InvalidOperationException("La vía no tiene moneda válida.");
    }

    private static void ValidateRequest(RevisionPlanPagoRequest r)
    {
        if (!r.Confirmado || r.SolicitudId == Guid.Empty || string.IsNullOrWhiteSpace(r.Comentario) || r.Comentario.Trim().Length > 2000)
            throw new InvalidOperationException("Se requieren confirmación explícita, identificador de solicitud y comentario de hasta 2000 caracteres.");
        Money(r.NuevoMontoActual);
        if (r.VersionVia == 0 || r.VersionPlan == 0 || r.CuotasOriginales == null || r.Cambios == null || r.Cambios.Count == 0 ||
            r.CuotasOriginales.Any(c => c == null || c.CuotaId <= 0 || c.Version == 0) ||
            r.CuotasOriginales.Select(c => c.CuotaId).Distinct().Count() != r.CuotasOriginales.Count)
            throw new InvalidOperationException("Se requieren versiones válidas, la colección original sin duplicados y cambios explícitos.");
        var ids = new HashSet<int>();
        foreach (var c in r.Cambios)
        {
            if (c == null || !Enum.IsDefined(c.Operacion)) throw new InvalidOperationException("Operación de cuota inválida.");
            if (c.Importe.HasValue) Money(c.Importe.Value);
            if (c.Vencimiento.HasValue && c.Vencimiento.Value.Year < 1900) throw new InvalidOperationException("Vencimiento inválido.");
            if (c.Operacion == CambioCuotaRevision.Agregar)
            {
                if (c.CuotaId != null || c.Version != null || c.Tipo != TipoCuota.Cuota || c.Numero is null or <= 0 || c.Importe == null || c.Vencimiento == null)
                    throw new InvalidOperationException("El alta requiere cuota ordinaria, número, importe y vencimiento, sin ID ni versión.");
            }
            else
            {
                if (c.CuotaId is null or <= 0 || c.Version is null or 0 || !ids.Add(c.CuotaId.Value) || c.Tipo != null || c.Numero != null)
                    throw new InvalidOperationException("Cada cuota existente requiere ID y versión, sin cambios de tipo o número ni operaciones duplicadas.");
                if (c.Operacion == CambioCuotaRevision.Modificar && c.Importe == null && c.Vencimiento == null ||
                    c.Operacion == CambioCuotaRevision.Retirar && (c.Importe != null || c.Vencimiento != null))
                    throw new InvalidOperationException("Los campos no corresponden a la operación solicitada.");
            }
        }
    }
    private static void Money(decimal amount)
    {
        if (amount < 0 || amount != Math.Round(amount, 2, MidpointRounding.AwayFromZero))
            throw new InvalidOperationException("El importe debe ser no negativo y tener como máximo dos decimales.");
    }
    private static void ValidateVersions(PlanPago plan, RevisionPlanPagoRequest r)
    {
        if (plan.Version != r.VersionPlan || plan.AcuerdoComercialVia.Version != r.VersionVia ||
            !plan.Cuotas.Select(c => c.Id).Order().SequenceEqual(r.CuotasOriginales.Select(c => c.CuotaId).Order()))
            throw Conflict("Cambió la vía, el plan o la colección de cuotas. Recargue la revisión.");
        var versions = r.CuotasOriginales.ToDictionary(c => c.CuotaId, c => c.Version);
        if (plan.Cuotas.Any(c => versions[c.Id] != c.Version)) throw Conflict("Una cuota cambió desde la preparación de la revisión.");
    }
    private static CuotaComercial? Last(PlanPago plan, HashSet<int>? removed = null) => plan.Cuotas
        .Where(c => c.TipoCuota == TipoCuota.Cuota && c.Estado != CuotaEstado.Anulada && !(removed?.Contains(c.Id) ?? false))
        .OrderByDescending(c => c.NumeroCuota).FirstOrDefault();

    private async Task<PreparacionRevisionPlanResponse> Prepare(PlanPago plan, CancellationToken ct)
    {
        var via = plan.AcuerdoComercialVia;
        // List.Contains keeps EF7 expression trees free of the newer Span overloads.
        var ids = plan.Cuotas.Select(c => c.Id).ToList();
        var payments = await _db.AplicacionesPagoComerciales.AsNoTracking().Where(a => a.CuotaComercialId != null && ids.Contains(a.CuotaComercialId.Value))
            .Select(a => new { Id = a.CuotaComercialId!.Value, a.PagoComercial.Estado }).ToListAsync(ct);
        var collections = await _db.CobranzasAplicacionesObligacion.AsNoTracking().Where(a => ids.Contains(a.CuotaComercialId))
            .Select(a => new { Id = a.CuotaComercialId, a.ImporteAplicado, a.AplicacionFactura.Cobranza.Estado, a.AplicacionFactura.Cobranza.MonedaCodigo }).ToListAsync(ct);
        var links = await _db.VinculacionesFacturaComerciales.AsNoTracking().Where(a => ids.Contains(a.CuotaComercialId)).ToListAsync(ct);
        var saleIds = links.Select(a => int.TryParse(a.FacturaExternaId, out var id) ? id : 0).Distinct().ToList();
        var sales = await _db.Ventas.AsNoTracking().Where(v => saleIds.Contains(v.Id)).Select(v => new { v.Id, v.Estado, v.MonedaCodigo }).ToDictionaryAsync(v => v.Id, ct);
        var adjustments = await _db.AjustesCuotaComerciales.AsNoTracking().Where(a => ids.Contains(a.CuotaComercialId)).Select(a => a.CuotaComercialId).ToListAsync(ct);
        var audited = await _db.RevisionesPlanesPagoDetalles.AsNoTracking().Where(d => d.PlanPagoId == plan.Id).Select(d => new { d.CuotaOriginalId, d.NumeroCuota }).ToListAsync(ct);
        var history = new HashSet<int>();
        await using (var command = _db.Database.GetDbConnection().CreateCommand())
        {
            command.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandText = "SELECT cuota_comercial_id FROM cuotas_comerciales_dependencias_historicas WHERE cuota_comercial_id = ANY(@ids)";
            command.Parameters.Add(new NpgsqlParameter("ids", ids.ToArray()));
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) history.Add(reader.GetInt32(0));
        }
        history.UnionWith(payments.Select(p => p.Id)); history.UnionWith(collections.Select(p => p.Id));
        history.UnionWith(links.Select(p => p.CuotaComercialId)); history.UnionWith(adjustments); history.UnionWith(audited.Select(d => d.CuotaOriginalId));
        var paid = 0m;
        if (via.ViaOperacion == ViaOperacion.Via1)
        {
            var valid = collections.Where(c => c.Estado == CobranzaEstado.Confirmada).ToList();
            if (valid.Any(c => !string.Equals(c.MonedaCodigo, via.MonedaCodigo, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Existen cobranzas en una moneda distinta de la vía; se requiere conciliación previa.");
            paid = valid.Sum(c => c.ImporteAplicado);
        }
        else
        {
            var valid = await _db.PagosComerciales.AsNoTracking().Where(p => p.AcuerdoComercialViaId == via.Id &&
                p.Estado != PagoEstado.Anulado && p.OrigenPago == OrigenPago.Comercial).Select(p => new { p.MonedaCodigo, p.ImporteTotal }).ToListAsync(ct);
            if (valid.Any(p => !string.Equals(p.MonedaCodigo, via.MonedaCodigo, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Existen pagos en una moneda distinta de la vía; se requiere conciliación previa.");
            paid = valid.Sum(p => p.ImporteTotal);
        }
        var last = Last(plan);
        var result = new PreparacionRevisionPlanResponse
        {
            AcuerdoComercialId = via.AcuerdoComercialId, AcuerdoComercialViaId = via.Id, PlanPagoId = plan.Id,
            TieneAnticipo = plan.TieneAnticipo, MontoAnticipo = plan.MontoAnticipo, CantidadCuotas = plan.CantidadCuotas,
            FechaPrimerVencimiento = plan.FechaPrimerVencimiento, Periodicidad = plan.Periodicidad,
            EstadoAcuerdo = via.AcuerdoComercial.Estado, EstadoVia = via.Estado, MonedaCodigo = via.MonedaCodigo,
            MontoOriginal = via.MontoOriginal, MontoActual = via.MontoActual, TotalVigente = plan.Cuotas.Where(c => c.Estado != CuotaEstado.Anulada).Sum(c => c.ImporteOriginal),
            TotalPagadoValido = paid, SaldoPendiente = Math.Max(via.MontoActual - paid, 0), SaldoAFavor = Math.Max(paid - via.MontoActual, 0),
            VersionVia = via.Version, VersionPlan = plan.Version, UltimaCuotaOrdinariaActivaId = last?.Id,
            SiguienteNumeroCuota = checked(plan.Cuotas.Select(c => c.NumeroCuota).Concat(audited.Select(d => d.NumeroCuota)).DefaultIfEmpty(0).Max() + 1)
        };
        foreach (var cuota in plan.Cuotas.OrderBy(c => c.NumeroCuota).ThenBy(c => c.Id))
        {
            var reasons = new List<string>();
            if (cuota.TipoCuota is not (TipoCuota.Cuota or TipoCuota.Anticipo)) reasons.Add("Este bloque permite modificar o retirar cuotas ordinarias y tratar el anticipo por separado.");
            if (cuota.Estado == CuotaEstado.Anulada) reasons.Add("La cuota ya está anulada.");
            if (cuota.ImportePagado != 0 || cuota.Estado is CuotaEstado.Pagada or CuotaEstado.Parcial) reasons.Add("La cuota tiene pago total o parcial; requiere reversión formal previa.");
            if (payments.Any(p => p.Id == cuota.Id && p.Estado != PagoEstado.Anulado)) reasons.Add("Tiene una aplicación de pago activa; requiere anulación previa.");
            if (collections.Any(c => c.Id == cuota.Id && c.Estado != CobranzaEstado.Anulada)) reasons.Add("Tiene una cobranza o aplicación activa; requiere anulación previa.");
            foreach (var link in links.Where(l => l.CuotaComercialId == cuota.Id))
            {
                if (!int.TryParse(link.FacturaExternaId, out var saleId) || !sales.TryGetValue(saleId, out var sale))
                    reasons.Add("Tiene una factura cuyo estado no puede verificarse; requiere conciliación previa.");
                else if (sale.Estado != VentaEstado.Anulada) reasons.Add("Tiene una factura activa o en borrador; requiere anulación formal previa.");
                else if (!string.Equals(sale.MonedaCodigo, via.MonedaCodigo, StringComparison.OrdinalIgnoreCase)) reasons.Add("La factura histórica tiene una moneda incompatible.");
            }
            var reason = reasons.Count == 0 ? null : string.Join(" ", reasons.Distinct());
            var retiro = reason ?? (cuota.TipoCuota == TipoCuota.Cuota && cuota.Id != last?.Id ? "Solo puede retirarse la última cuota ordinaria activa." : null);
            result.Cuotas.Add(new() { Id = cuota.Id, Numero = cuota.NumeroCuota, Tipo = cuota.TipoCuota, Importe = cuota.ImporteOriginal,
                Vencimiento = cuota.FechaVencimiento, Estado = cuota.Estado, Version = cuota.Version, TieneHistoria = history.Contains(cuota.Id),
                PuedeModificar = reason == null, PuedeRetirar = retiro == null, MotivoBloqueoModificacion = reason, MotivoBloqueoRetiro = retiro });
        }
        return result;
    }

    private static RevisionPlanPagoDetalle Snapshot(CuotaComercial c, OperacionRevisionPlanPago op) => new()
    {
        PlanPagoId = c.PlanPagoId, CuotaComercialId = c.Id, CuotaOriginalId = c.Id, NumeroCuota = c.NumeroCuota, TipoCuota = c.TipoCuota,
        Operacion = op, ImporteAnterior = c.ImporteOriginal, VencimientoAnterior = c.FechaVencimiento, EstadoAnterior = c.Estado,
        ImportePagadoAnterior = c.ImportePagado, SaldoPendienteAnterior = c.SaldoPendiente
    };
    private static void Complete(RevisionPlanPagoDetalle d, CuotaComercial c)
    { d.ImporteNuevo = c.ImporteOriginal; d.VencimientoNuevo = c.FechaVencimiento; d.EstadoNuevo = c.Estado; }
    private static RevisionPlanPagoResponse Map(RevisionPlanPago r) => new()
    {
        Id = r.Id, SolicitudId = r.SolicitudId, AcuerdoComercialId = r.AcuerdoComercialId, AcuerdoComercialViaId = r.AcuerdoComercialViaId,
        PlanPagoId = r.PlanPagoId, MontoAnterior = r.MontoAnterior, MontoNuevo = r.MontoNuevo, Diferencia = r.Diferencia,
        Comentario = r.Comentario, UsuarioId = r.UsuarioId, Usuario = r.Usuario, Fecha = r.Fecha, TipoRevision = r.TipoRevision, MonedaCodigo = r.MonedaCodigo,
        Detalles = r.Detalles.OrderBy(d => d.NumeroCuota).Select(d => new RevisionPlanPagoDetalleResponse
        { Id = d.Id, CuotaComercialId = d.CuotaComercialId, CuotaOriginalId = d.CuotaOriginalId, NumeroCuota = d.NumeroCuota, TipoCuota = d.TipoCuota,
            Operacion = d.Operacion, ImporteAnterior = d.ImporteAnterior, ImporteNuevo = d.ImporteNuevo, VencimientoAnterior = d.VencimientoAnterior,
            VencimientoNuevo = d.VencimientoNuevo, EstadoAnterior = d.EstadoAnterior, EstadoNuevo = d.EstadoNuevo,
            ImportePagadoAnterior = d.ImportePagadoAnterior, SaldoPendienteAnterior = d.SaldoPendienteAnterior }).ToList()
    };
}

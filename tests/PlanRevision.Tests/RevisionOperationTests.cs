using System.Security.Claims;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

static class RevisionOperationTests
{
    private static int _saleNumber = 100;
    internal static IHttpContextAccessor Identity(string? role = "Admin") => new HttpContextAccessor
    {
        HttpContext = new DefaultHttpContext { User = role == null ? new ClaimsPrincipal() : new ClaimsPrincipal(new ClaimsIdentity(new[]
        { new Claim(ClaimTypes.NameIdentifier, "123"), new Claim(ClaimTypes.Name, "admin-test"), new Claim(ClaimTypes.Role, role) }, "test")) }
    };
    internal static RevisionPlanPagoService Service(AppDbContext db) => new(db, Identity());
    internal static async Task<PlanPago> Seed(Func<AppDbContext> create, int count = 5, bool advance = false)
    {
        await using var db = create();
        var plan = Fixtures.NewPlan();
        plan.Cuotas.Clear();
        plan.AcuerdoComercialVia.ViaOperacion = ViaOperacion.Via2;
        for (var i = advance ? 0 : 1; i <= count; i++) plan.Cuotas.Add(new CuotaComercial
        { NumeroCuota = i, TipoCuota = i == 0 ? TipoCuota.Anticipo : TipoCuota.Cuota, ImporteOriginal = 100, SaldoPendiente = 100,
            FechaVencimiento = CommercialCalendar.Today.AddMonths(i + 1), Estado = CuotaEstado.Pendiente });
        plan.CantidadCuotas = count; plan.TieneAnticipo = advance; plan.MontoAnticipo = advance ? 100 : 0;
        plan.AcuerdoComercialVia.MontoOriginal = plan.AcuerdoComercialVia.MontoActual = plan.Cuotas.Sum(c => c.ImporteOriginal);
        db.PlanesPago.Add(plan); await db.SaveChangesAsync(); return plan;
    }
    internal static async Task<PreparacionRevisionPlanResponse> Prepare(Func<AppDbContext> create, PlanPago p)
    {
        await using var db = create();
        return await Service(db).PrepararAsync(p.AcuerdoComercialVia.AcuerdoComercialId, p.AcuerdoComercialViaId, p.Id);
    }
    internal static RevisionPlanPagoRequest Request(PreparacionRevisionPlanResponse p, params CambioCuotaRevisionRequest[] changes) => new()
    {
        AcuerdoComercialId = p.AcuerdoComercialId, AcuerdoComercialViaId = p.AcuerdoComercialViaId, PlanPagoId = p.PlanPagoId,
        NuevoMontoActual = p.MontoActual, SolicitudId = Guid.NewGuid(), Comentario = "Revisión confirmada", Confirmado = true,
        VersionVia = p.VersionVia, VersionPlan = p.VersionPlan,
        CuotasOriginales = p.Cuotas.Select(c => new VersionCuotaRevision { CuotaId = c.Id, Version = c.Version }).ToList(), Cambios = changes.ToList()
    };
    internal static CambioCuotaRevisionRequest Remove(CuotaPreparacionRevisionResponse c) => new() { Operacion = CambioCuotaRevision.Retirar, CuotaId = c.Id, Version = c.Version };
    internal static CambioCuotaRevisionRequest Modify(CuotaPreparacionRevisionResponse c, decimal? amount = null, DateTime? date = null) => new()
    { Operacion = CambioCuotaRevision.Modificar, CuotaId = c.Id, Version = c.Version, Importe = amount, Vencimiento = date };
    internal static CambioCuotaRevisionRequest Add(int number) => new() { Operacion = CambioCuotaRevision.Agregar, Tipo = TipoCuota.Cuota, Numero = number, Importe = 100, Vencimiento = CommercialCalendar.Today.AddMonths(number + 1) };
    internal static async Task<ConfirmacionRevisionPlanResponse> Confirm(Func<AppDbContext> create, RevisionPlanPagoRequest r)
    { await using var db = create(); return await Service(db).ConfirmarAsync(r); }

    public static async Task RunAsync(Func<AppDbContext> create, string connectionString, Action<bool, string> check)
    {
        async Task Reject(RevisionPlanPagoRequest r, string name, bool conflict = false)
        {
            await using var verify = create();
            var before = await verify.CuotasComerciales.AsNoTracking().Where(c => c.PlanPagoId == r.PlanPagoId).OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.ImporteOriginal, c.FechaVencimiento, c.Estado }).ToListAsync();
            try { await Confirm(create, r); throw new Exception("Expected rejection: " + name); }
            catch (InvalidOperationException ex) when (!conflict || ex is RevisionPlanConflictException) { check(true, name); }
            var after = await verify.CuotasComerciales.AsNoTracking().Where(c => c.PlanPagoId == r.PlanPagoId).OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.ImporteOriginal, c.FechaVencimiento, c.Estado }).ToListAsync();
            check(before.SequenceEqual(after) && !await verify.RevisionesPlanesPago.AnyAsync(a => a.SolicitudId == r.SolicitudId), name + ": no partial changes/audit");
        }
        var plan = await Seed(create);
        foreach (var role in new string?[] { null, "User" })
        {
            await using var db = create();
            try { await new RevisionPlanPagoService(db, Identity(role)).PrepararAsync(1, 1, 1); throw new Exception("Expected denial"); }
            catch (UnauthorizedAccessException) { check(true, "service denies " + (role ?? "unauthenticated")); }
        }
        var prepared = await Prepare(create, plan);
        check(prepared.Cuotas.Count == 5 && prepared.Cuotas.Last().PuedeRetirar && !prepared.Cuotas.First().PuedeRetirar, "Admin prepares approved plan with per-quota reasons");
        foreach (var mode in new[] { "amount", "date", "both" })
        {
            prepared = await Prepare(create, plan);
            var c = prepared.Cuotas.First();
            var amount = mode == "date" ? (decimal?)null : c.Importe - 10;
            var date = mode == "amount" ? (DateTime?)null : c.Vencimiento.AddDays(3);
            var request = Request(prepared, Modify(c, amount, date));
            request.NuevoMontoActual -= amount.HasValue ? 10 : 0;
            var start = DateTime.UtcNow;
            var result = await Confirm(create, request);
            var detail = result.Revision.Detalles.Single();
            check(detail.ImporteAnterior == c.Importe && detail.ImporteNuevo == (amount ?? c.Importe) && detail.VencimientoAnterior == c.Vencimiento && detail.VencimientoNuevo == (date ?? c.Vencimiento), "modify " + mode + " preserves old/new audit");
            check(result.EstadoActual.MontoOriginal == 500 && result.EstadoActual.EstadoVia == AcuerdoEstado.Aprobado && result.EstadoActual.EstadoAcuerdo == AcuerdoEstado.Aprobado, "revision preserves approval and MontoOriginal: " + mode);
            check(result.Revision.UsuarioId == "123" && result.Revision.Usuario == "admin-test" && result.Revision.Fecha >= start && result.Revision.Fecha <= DateTime.UtcNow && result.Revision.MonedaCodigo == "ARS" && result.Revision.Comentario == request.Comentario, "complete header and server timestamp: " + mode);
            var replay = await Confirm(create, request);
            check(replay.SolicitudYaProcesada && replay.Revision.Id == result.Revision.Id, "same request replays committed audit: " + mode);
        }
        prepared = await Prepare(create, plan);
        var invalid = Request(prepared, Modify(prepared.Cuotas.First(), 1));
        await Reject(invalid, "inconsistent total rollback");
        foreach (var kind in new[] { "comment", "confirmation", "version", "negative", "precision" })
        {
            invalid = Request(prepared, Modify(prepared.Cuotas.First(), date: prepared.Cuotas.First().Vencimiento.AddDays(1)));
            switch (kind) { case "comment": invalid.Comentario = " \t "; break; case "confirmation": invalid.Confirmado = false; break;
                case "version": invalid.VersionPlan = 0; break; case "negative": invalid.Cambios[0].Importe = -1; break; case "precision": invalid.Cambios[0].Importe = 1.001m; break; }
            await Reject(invalid, "reject invalid " + kind);
        }
        await Reject(Request(prepared, Remove(prepared.Cuotas[1])), "cannot withdraw intermediate quota", true);

        var tail = await Seed(create);
        foreach (var number in new[] { 5, 4, 3 })
        {
            var p = await Prepare(create, tail); var c = p.Cuotas.Single(c => c.Numero == number);
            var r = Request(p, Remove(c)); r.NuevoMontoActual -= 100;
            var result = await Confirm(create, r);
            check(result.Revision.Detalles.Single().CuotaComercialId == null && result.Revision.Detalles.Single().CuotaOriginalId == c.Id &&
                result.Revision.Detalles.Single().Operacion == OperacionRevisionPlanPago.Eliminada && result.EstadoActual.Cuotas.All(c => c.Numero < number), "physical tail withdrawal and durable snapshot: " + number);
        }
        var pTail = await Prepare(create, tail);
        check(pTail.Cuotas.Select(c => c.Numero).SequenceEqual(new[] { 1, 2 }) && pTail.SiguienteNumeroCuota == 6, "no renumbering and historical high-water number survives physical deletion");
        var reused = Request(pTail, Add(3)); reused.NuevoMontoActual += 100;
        await Reject(reused, "cannot reuse physically deleted historical number");
        var multiple = Request(pTail, Add(7), Add(6)); multiple.NuevoMontoActual += 200;
        var added = await Confirm(create, multiple);
        check(added.EstadoActual.Cuotas.Select(c => c.Numero).SequenceEqual(new[] { 1, 2, 6, 7 }) && added.Revision.Detalles.All(d => d.Operacion == OperacionRevisionPlanPago.Agregada && d.ImporteAnterior == null && d.CuotaComercialId > 0), "multiple additions sorted at historical tail with audit");
        var dup = Request(added.EstadoActual, Add(7)); dup.NuevoMontoActual += 100;
        await Reject(dup, "duplicate number rejected");
        var batch = await Seed(create, 5, true);
        var pb = await Prepare(create, batch);
        var rb = Request(pb, Remove(pb.Cuotas.Single(c => c.Numero == 3)), Remove(pb.Cuotas.Single(c => c.Numero == 5)), Remove(pb.Cuotas.Single(c => c.Numero == 4)));
        rb.NuevoMontoActual -= 300;
        var resultBatch = await Confirm(create, rb);
        check(resultBatch.EstadoActual.Cuotas.Select(c => c.Numero).SequenceEqual(new[] { 0, 1, 2 }), "batch withdrawal sorts descending and excludes advance from sequence");
        var advance = Request(resultBatch.EstadoActual, Remove(resultBatch.EstadoActual.Cuotas.Single(c => c.Numero == 0))); advance.NuevoMontoActual -= 100;
        check((await Confirm(create, advance)).EstadoActual.Cuotas.Count == 2, "advance withdrawal handled independently");

        foreach (var dep in new[] { "invoice-active", "invoice-cancelled", "payment-active", "payment-cancelled", "collection-active", "collection-cancelled", "partial", "unlinked-history" })
        {
            var dp = await Seed(create); var quota = dp.Cuotas.OrderBy(c => c.NumeroCuota).Last();
            await AddDependency(create, dp, quota.Id, dep);
            var pd = await Prepare(create, dp);
            var cd = pd.Cuotas.Last();
            var rd = Request(pd, Remove(cd)); rd.NuevoMontoActual -= 100;
            var blocked = dep.EndsWith("active") || dep == "partial";
            if (blocked)
            {
                check(!cd.PuedeModificar && !cd.PuedeRetirar && cd.MotivoBloqueoRetiro != null, dep + " has concrete blocking reason");
                await Reject(rd, "withdraw rejects " + dep, true);
                await Reject(Request(pd, Modify(cd, date: cd.Vencimiento.AddDays(1))), "modify rejects " + dep, true);
                var skip = Request(pd, Remove(pd.Cuotas[3])); skip.NuevoMontoActual -= 100;
                await Reject(skip, "cannot skip blocked tail: " + dep, true);
            }
            else
            {
                check(cd.TieneHistoria && cd.PuedeModificar, dep + " historical evidence is visible");
                var modified = await Confirm(create, Request(pd, Modify(cd, date: cd.Vencimiento.AddDays(1))));
                var retire = Request(modified.EstadoActual, Remove(modified.EstadoActual.Cuotas.Last())); retire.NuevoMontoActual -= 100;
                var result = await Confirm(create, retire);
                check(result.EstadoActual.Cuotas.Last().Estado == CuotaEstado.Anulada && result.Revision.Detalles.Single().Operacion == OperacionRevisionPlanPago.Excluida && result.Revision.Detalles.Single().CuotaComercialId == quota.Id, "logical withdrawal preserves history: " + dep);
                var priorTail = result.EstadoActual.Cuotas.Single(c => c.Numero == 4);
                check(result.EstadoActual.UltimaCuotaOrdinariaActivaId == priorTail.Id && priorTail.PuedeRetirar, "logical withdrawal exposes previous active tail: " + dep);
            }
        }

        foreach (var amount in new[] { 50m, 100m, 150m })
        {
            var sp = await Seed(create, 1);
            int paymentId;
            await using (var db = create())
            {
                var payment = Fixtures.Payment(sp); payment.AcuerdoComercial = null!; payment.AcuerdoComercialVia = null!;
                payment.AcuerdoComercialId = sp.AcuerdoComercialVia.AcuerdoComercialId; payment.AcuerdoComercialViaId = sp.AcuerdoComercialViaId;
                payment.ImporteTotal = amount; db.PagosComerciales.Add(payment); await db.SaveChangesAsync(); paymentId = payment.Id;
            }
            var ps = await Prepare(create, sp);
            var rs = await Confirm(create, Request(ps, Modify(ps.Cuotas.Single(), date: ps.Cuotas.Single().Vencimiento.AddDays(1))));
            check(rs.EstadoActual.TotalPagadoValido == amount && rs.EstadoActual.SaldoAFavor == Math.Max(amount - 100, 0) && rs.EstadoActual.SaldoPendiente == Math.Max(100 - amount, 0), "dynamic payment balance: " + amount);
            await using (var db = create()) { var pay = await db.PagosComerciales.SingleAsync(p => p.Id == paymentId); pay.Estado = PagoEstado.Anulado; await db.SaveChangesAsync(); }
            check((await Prepare(create, sp)).TotalPagadoValido == 0, "subsequent cancellation recalculates balance: " + amount);
        }
        var obsolete = await Seed(create, 2); var po = await Prepare(create, obsolete);
        await using (var db = create()) { var c = await db.CuotasComerciales.SingleAsync(c => c.Id == po.Cuotas.Last().Id); c.FechaVencimiento = c.FechaVencimiento.AddDays(1); await db.SaveChangesAsync(); }
        await Reject(Request(po, Modify(po.Cuotas.Last(), date: po.Cuotas.Last().Vencimiento.AddDays(2))), "quota changed after preparation", true);
        po = await Prepare(create, obsolete);
        await using (var db = create()) { db.CuotasComerciales.Add(new CuotaComercial { PlanPagoId = obsolete.Id, NumeroCuota = 3, TipoCuota = TipoCuota.Cuota, ImporteOriginal = 0, FechaVencimiento = CommercialCalendar.Today }); await db.SaveChangesAsync(); }
        await Reject(Request(po, Remove(po.Cuotas.Last())), "collection changed and quota is no longer last", true);
        var race = await Seed(create, 1); var pr = await Prepare(create, race);
        var r1 = Request(pr, Modify(pr.Cuotas.Single(), date: pr.Cuotas.Single().Vencimiento.AddDays(1)));
        var r2 = Request(pr, Modify(pr.Cuotas.Single(), date: pr.Cuotas.Single().Vencimiento.AddDays(2)));
        async Task<bool> Race(RevisionPlanPagoRequest r) { try { await Confirm(create, r); return true; } catch (RevisionPlanConflictException) { return false; } }
        var raceResult = await Task.WhenAll(Race(r1), Race(r2));
        check(raceResult.Count(x => x) == 1, "two simultaneous administrators: one success and one controlled conflict");
        var retryPlan = await Seed(create, 1); var retryPrep = await Prepare(create, retryPlan);
        var retryRequest = Request(retryPrep, Modify(retryPrep.Cuotas.Single(), date: retryPrep.Cuotas.Single().Vencimiento.AddDays(1)));
        var retries = await Task.WhenAll(Confirm(create, retryRequest), Confirm(create, retryRequest));
        check(retries[0].Revision.Id == retries[1].Revision.Id && retries.Count(r => r.SolicitudYaProcesada) == 1, "simultaneous same-key requests create one revision");
        await AdditionalTests(create, check);
        await RevisionHttpTests.RunAsync(create, connectionString, check);
    }

    internal static async Task AddDependency(Func<AppDbContext> create, PlanPago p, int quotaId, string kind)
    {
        await using var db = create();
        if (kind == "partial")
        { var c = await db.CuotasComerciales.SingleAsync(c => c.Id == quotaId); c.ImportePagado = 10; c.SaldoPendiente = 90; c.Estado = CuotaEstado.Parcial; }
        else if (kind.StartsWith("payment"))
        {
            db.AplicacionesPagoComerciales.Add(new AplicacionPagoComercial { CuotaComercialId = quotaId, ImporteAplicado = 10, FechaAplicacion = DateTime.UtcNow, UsuarioAplicacion = "test",
                PagoComercial = new PagoComercial { AcuerdoComercialId = p.AcuerdoComercialVia.AcuerdoComercialId, AcuerdoComercialViaId = p.AcuerdoComercialViaId,
                    ClienteExternoId = "test", ObraExternaId = "test", FechaPago = DateTime.UtcNow, FechaAlta = DateTime.UtcNow, UsuarioAlta = "test", MedioPago = "test", ImporteTotal = 10,
                    Estado = kind.EndsWith("cancelled") ? PagoEstado.Anulado : PagoEstado.Aplicado } });
        }
        else if (kind.StartsWith("invoice") || kind == "unlinked-history")
        {
            var sale = await Sale(db, kind.EndsWith("cancelled") ? VentaEstado.Anulada : VentaEstado.Confirmada);
            var link = new VinculacionFacturaComercial { CuotaComercialId = quotaId, FacturaExternaId = sale.Id.ToString(), NumeroFactura = "test", ImporteVinculado = 10, FechaVinculacion = DateTime.UtcNow };
            db.VinculacionesFacturaComerciales.Add(link); await db.SaveChangesAsync();
            if (kind == "unlinked-history") db.VinculacionesFacturaComerciales.Remove(link);
        }
        else if (kind.StartsWith("collection"))
        {
            var sale = await Sale(db, VentaEstado.Confirmada);
            db.CobranzasAplicacionesObligacion.Add(new CobranzaAplicacionObligacion { CuotaComercialId = quotaId, TipoObligacion = "Cuota", ImporteAplicado = 10, FechaAlta = DateTime.UtcNow, UsuarioAlta = "test",
                AplicacionFactura = new CobranzaAplicacionFactura { VentaId = sale.Id, ImporteAplicado = 10, FechaAlta = DateTime.UtcNow, UsuarioAlta = "test",
                    Cobranza = new Cobranza { ClienteExternoId = "test", Fecha = DateTime.UtcNow, FechaAlta = DateTime.UtcNow, UsuarioAlta = "test", ImporteTotal = 10,
                        Estado = kind.EndsWith("cancelled") ? CobranzaEstado.Anulada : CobranzaEstado.Confirmada } } });
        }
        await db.SaveChangesAsync();
    }
    private static async Task<Venta> Sale(AppDbContext db, VentaEstado state)
    {
        var sale = new Venta { TipoComprobanteVentaId = await db.TiposComprobanteVenta.Select(t => t.Id).FirstAsync(), ClienteExternoId = "test", ObraExternaId = "test",
            FechaComprobante = DateTime.UtcNow, FechaAlta = DateTime.UtcNow, UsuarioAlta = "test", Estado = state, Total = 10,
            NumeroComprobante = Interlocked.Increment(ref _saleNumber) };
        db.Ventas.Add(sale); await db.SaveChangesAsync(); return sale;
    }

    private static async Task AdditionalTests(Func<AppDbContext> create, Action<bool, string> check)
    {
        var p = await Seed(create, 2);
        var prep = await Prepare(create, p);
        await using (var setup = create())
        {
            // Failure AFTER the service's first SaveChanges must undo amounts, deletions and additions.
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION test_revision_abort() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF NEW.comentario = 'test-abort-audit' THEN RAISE EXCEPTION 'test deadlock' USING ERRCODE='40P01'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER test_revision_abort BEFORE INSERT ON revisiones_planes_pago FOR EACH ROW EXECUTE FUNCTION test_revision_abort();
                """);
            try
            {
                var r = Request(prep, Modify(prep.Cuotas.First(), 50), Remove(prep.Cuotas.Last()), Add(3));
                r.NuevoMontoActual = 150; r.Comentario = "test-abort-audit";
                try { await Confirm(create, r); throw new Exception("Expected deadlock conflict"); }
                catch (RevisionPlanConflictException) { check(true, "PostgreSQL deadlock SQLSTATE becomes controlled conflict"); }
                var after = await Prepare(create, p);
                check(after.MontoActual == 200 && after.Cuotas.Select(c => c.Id).SequenceEqual(prep.Cuotas.Select(c => c.Id)) &&
                    after.Cuotas.All(c => c.Importe == 100) && !await setup.RevisionesPlanesPago.AnyAsync(a => a.SolicitudId == r.SolicitudId),
                    "audit failure rolls back already-saved amount, physical delete and addition");
            }
            finally { await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER test_revision_abort ON revisiones_planes_pago; DROP FUNCTION test_revision_abort();"); }
        }
        // Via1 counts collection allocations exactly once and ignores mirrored commercial payments.
        var via1 = await Seed(create, 1);
        await using (var db = create())
        { var v = await db.AcuerdosComercialesVias.SingleAsync(v => v.Id == via1.AcuerdoComercialViaId); v.ViaOperacion = ViaOperacion.Via1; await db.SaveChangesAsync(); }
        await AddDependency(create, via1, via1.Cuotas.Single().Id, "collection-active");
        await AddDependency(create, via1, via1.Cuotas.Single().Id, "payment-active");
        check((await Prepare(create, via1)).TotalPagadoValido == 10, "Via1 counts collections once without mirrored commercial payments");
        await using (var db = create())
        {
            var collection = await db.CobranzasAplicacionesObligacion.Where(a => a.CuotaComercialId == via1.Cuotas.Single().Id).Select(a => a.AplicacionFactura.Cobranza).SingleAsync();
            collection.Estado = CobranzaEstado.Anulada; await db.SaveChangesAsync();
        }
        check((await Prepare(create, via1)).TotalPagadoValido == 0, "Via1 cancellation recalculates valid collections");
        var currency = await Seed(create, 1);
        await AddDependency(create, currency, currency.Cuotas.Single().Id, "payment-active");
        await using (var db = create())
        { var payment = await db.PagosComerciales.SingleAsync(x => x.AcuerdoComercialViaId == currency.AcuerdoComercialViaId); payment.MonedaCodigo = "USD"; await db.SaveChangesAsync(); }
        try { await Prepare(create, currency); throw new Exception("Expected currency rejection"); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("moneda")) { check(true, "inconsistent currency rejected without conversion or summing currencies"); }
        var draft = await Seed(create, 1);
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == draft.AcuerdoComercialViaId);
            via.Estado = via.AcuerdoComercial.Estado = AcuerdoEstado.Borrador; await db.SaveChangesAsync();
        }
        try { await Prepare(create, draft); throw new Exception("Expected draft rejection"); }
        catch (RevisionPlanConflictException) { check(true, "draft is not an approved-plan revision"); }
        var foreign = await Seed(create, 1);
        var fp = await Prepare(create, foreign);
        var wrong = Request(prep, Modify(fp.Cuotas.Single(), 100));
        try { await Confirm(create, wrong); throw new Exception("Expected foreign quota rejection"); }
        catch (RevisionPlanConflictException) { check(true, "quota from another plan rejected by operation"); }
    }
}

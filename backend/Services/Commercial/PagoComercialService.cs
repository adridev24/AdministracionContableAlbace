using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Commercial
{
    public class PagoComercialService : IPagoComercialService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public PagoComercialService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<PagoComercialResponse> RegistrarPagoAsync(CreatePagoComercialRequest request)
        {
            if (request.ImporteTotal <= 0)
            {
                throw new InvalidOperationException("El importe del pago debe ser mayor a cero.");
            }

            var via = await _db.AcuerdosComercialesVias
                .Include(v => v.AcuerdoComercial)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.Id == request.AcuerdoComercialViaId);

            if (via == null)
            {
                throw new InvalidOperationException("Via comercial no encontrada.");
            }

            if (via.AcuerdoComercialId != request.AcuerdoComercialId)
            {
                throw new InvalidOperationException("La via no pertenece al acuerdo comercial informado.");
            }

            if (via.ViaOperacion != ViaOperacion.Via2)
            {
                throw new InvalidOperationException("Los pagos de Via1 se registran desde el modulo Ventas.");
            }

            var moneda = NormalizeCurrency(request.MonedaCodigo);
            if (via.MonedaCodigo != moneda)
            {
                throw new InvalidOperationException("La moneda del pago no coincide con la moneda de la via.");
            }

            if (via.Estado == AcuerdoEstado.Anulado || via.AcuerdoComercial.Estado == AcuerdoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede registrar pago para una via anulada.");
            }

            var saldoVia = via.MontoActual - via.Pagos.Where(p => p.Estado != PagoEstado.Anulado).Sum(p => p.ImporteTotal);
            if (request.ImporteTotal > saldoVia)
            {
                throw new InvalidOperationException("No se puede registrar un pago mayor al saldo pendiente de la via.");
            }

            var pago = new PagoComercial
            {
                ClienteExternoId = request.ClienteExternoId,
                ObraExternaId = request.ObraExternaId,
                AcuerdoComercialId = request.AcuerdoComercialId,
                AcuerdoComercialViaId = request.AcuerdoComercialViaId,
                FechaPago = EnsureUtc(request.FechaPago),
                MonedaCodigo = moneda,
                ImporteTotal = request.ImporteTotal,
                MedioPago = request.MedioPago,
                TipoImputacion = request.TipoImputacion,
                OrigenPago = OrigenPago.Comercial,
                Observaciones = request.Observaciones,
                Estado = PagoEstado.Registrado,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            _db.PagosComerciales.Add(pago);
            await _db.SaveChangesAsync();

            var aplicaciones = request.Aplicaciones.Any()
                ? request.Aplicaciones
                : new List<AplicacionPagoRequest>
                {
                    new()
                    {
                        ImporteAplicado = request.ImporteTotal,
                        TipoImputacion = request.TipoImputacion,
                        Observaciones = request.Observaciones
                    }
                };

            return MapPago(await AplicarPagoInternoAsync(pago, aplicaciones));
        }

        public async Task<PagoComercialResponse> AplicarPagoAsync(int pagoId, AplicarPagoRequest request)
        {
            var pago = await _db.PagosComerciales
                .Include(p => p.AcuerdoComercialVia)
                .Include(p => p.Aplicaciones)
                .FirstOrDefaultAsync(p => p.Id == pagoId);

            if (pago == null)
            {
                throw new InvalidOperationException("Pago comercial no encontrado.");
            }

            if (pago.Estado == PagoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede aplicar un pago anulado.");
            }

            if (!request.Aplicaciones.Any())
            {
                throw new InvalidOperationException("Debe incluir al menos una aplicacion de pago.");
            }

            return MapPago(await AplicarPagoInternoAsync(pago, request.Aplicaciones));
        }

        public async Task<PagoComercialResponse?> GetPagoAsync(int pagoId)
        {
            var pago = await GetPagoQuery()
                .FirstOrDefaultAsync(p => p.Id == pagoId);

            return pago == null ? null : MapPago(pago);
        }

        public async Task<PagoComercialResponse> AnularPagoAsync(int pagoId, AnularPagoComercialRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                throw new InvalidOperationException("Debe indicar un motivo de anulacion.");
            }

            var pago = await GetPagoQuery()
                .FirstOrDefaultAsync(p => p.Id == pagoId);

            if (pago == null)
            {
                throw new InvalidOperationException("Pago comercial no encontrado.");
            }

            if (pago.AcuerdoComercialVia.ViaOperacion != ViaOperacion.Via2)
            {
                throw new InvalidOperationException("Solo se pueden anular pagos comerciales de Via2 desde este circuito.");
            }

            if (pago.Estado == PagoEstado.Anulado)
            {
                throw new InvalidOperationException("El pago ya se encuentra anulado.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            foreach (var aplicacion in pago.Aplicaciones)
            {
                if (aplicacion.HitoComercialVia != null)
                {
                    aplicacion.HitoComercialVia.ImporteAplicado = Math.Max(aplicacion.HitoComercialVia.ImporteAplicado - aplicacion.ImporteAplicado, 0);
                    aplicacion.HitoComercialVia.Estado = aplicacion.HitoComercialVia.ImporteAplicado <= 0
                        ? HitoEstado.Pendiente
                        : aplicacion.HitoComercialVia.ImporteEstimado > 0 && aplicacion.HitoComercialVia.ImporteAplicado >= aplicacion.HitoComercialVia.ImporteEstimado
                            ? HitoEstado.Cumplido
                            : HitoEstado.Parcial;
                }

                if (aplicacion.CuotaComercial != null)
                {
                    var cuotaVia = aplicacion.CuotaComercial.PlanPago.AcuerdoComercialVia;
                    if (cuotaVia.ViaOperacion != ViaOperacion.Via2 || cuotaVia.Id != pago.AcuerdoComercialViaId)
                    {
                        throw new InvalidOperationException("La aplicacion a cuota no pertenece a la misma Via2 del pago.");
                    }

                    aplicacion.CuotaComercial.ImportePagado = Math.Max(aplicacion.CuotaComercial.ImportePagado - aplicacion.ImporteAplicado, 0);
                    aplicacion.CuotaComercial.SaldoPendiente = Math.Min(aplicacion.CuotaComercial.SaldoPendiente + aplicacion.ImporteAplicado, aplicacion.CuotaComercial.ImporteOriginal);
                    UpdateCuotaEstado(aplicacion.CuotaComercial);
                }
            }

            pago.Estado = PagoEstado.Anulado;
            pago.FechaAnulacion = DateTime.UtcNow;
            pago.UsuarioAnulacion = _userContext.UserName;
            pago.MotivoAnulacion = request.Motivo.Trim();

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapPago(pago);
        }

        public async Task<IEnumerable<AplicacionPagoResponse>> GetAplicacionesPorCuotaAsync(int cuotaId)
        {
            var aplicaciones = await _db.AplicacionesPagoComerciales
                .Include(a => a.PagoComercial)
                .Where(a => a.CuotaComercialId == cuotaId && a.PagoComercial.Estado != PagoEstado.Anulado)
                .ToListAsync();

            return aplicaciones.Select(MapAplicacion).ToList();
        }

        private async Task<PagoComercial> AplicarPagoInternoAsync(PagoComercial pago, List<AplicacionPagoRequest> aplicacionRequests)
        {
            var totalYaAplicado = pago.Aplicaciones.Sum(x => x.ImporteAplicado);
            var totalSolicitado = aplicacionRequests.Sum(r => r.ImporteAplicado);
            var saldoPago = pago.ImporteTotal - totalYaAplicado;

            if (totalSolicitado <= 0)
            {
                throw new InvalidOperationException("El importe aplicado debe ser mayor a cero.");
            }

            if (totalSolicitado > saldoPago)
            {
                throw new InvalidOperationException("No se puede aplicar mas importe que el total disponible del pago.");
            }

            var cuotaIds = aplicacionRequests
                .Where(a => a.CuotaComercialId.HasValue)
                .Select(a => a.CuotaComercialId!.Value)
                .Distinct()
                .ToList();
            var cuotas = await _db.CuotasComerciales
                .Include(c => c.PlanPago)
                    .ThenInclude(p => p.AcuerdoComercialVia)
                .Where(c => cuotaIds.Contains(c.Id))
                .ToListAsync();

            if (cuotas.Count != cuotaIds.Count)
            {
                throw new InvalidOperationException("Algunas cuotas comerciales no se encontraron.");
            }

            var hitoIds = aplicacionRequests
                .Where(a => a.HitoComercialViaId.HasValue)
                .Select(a => a.HitoComercialViaId!.Value)
                .Distinct()
                .ToList();
            var hitos = await _db.HitosComercialesVias
                .Where(h => hitoIds.Contains(h.Id))
                .ToListAsync();

            if (hitos.Count != hitoIds.Count)
            {
                throw new InvalidOperationException("Algunos hitos comerciales no se encontraron.");
            }

            foreach (var request in aplicacionRequests)
            {
                CuotaComercial? cuota = null;
                HitoComercialVia? hito = null;

                if (request.TipoImputacion == TipoImputacion.Cuota)
                {
                    if (!request.CuotaComercialId.HasValue)
                    {
                        throw new InvalidOperationException("Debe indicar una cuota para imputar a cuota.");
                    }

                    cuota = cuotas.Single(c => c.Id == request.CuotaComercialId.Value);
                    var via = cuota.PlanPago.AcuerdoComercialVia;

                    if (via.Id != pago.AcuerdoComercialViaId)
                    {
                        throw new InvalidOperationException("La cuota no pertenece a la misma via comercial del pago.");
                    }

                    if (via.MonedaCodigo != pago.MonedaCodigo)
                    {
                        throw new InvalidOperationException("La moneda de la cuota no coincide con la moneda del pago.");
                    }

                    if (request.ImporteAplicado > cuota.SaldoPendiente)
                    {
                        throw new InvalidOperationException($"No se puede aplicar mas importe que el saldo pendiente de la cuota {cuota.Id}.");
                    }

                    cuota.ImportePagado += request.ImporteAplicado;
                    cuota.SaldoPendiente = Math.Max(cuota.SaldoPendiente - request.ImporteAplicado, 0);
                    UpdateCuotaEstado(cuota);
                }
                else if (request.TipoImputacion == TipoImputacion.Hito)
                {
                    if (!request.HitoComercialViaId.HasValue)
                    {
                        throw new InvalidOperationException("Debe indicar un hito para imputar a hito.");
                    }

                    hito = hitos.Single(h => h.Id == request.HitoComercialViaId.Value);
                    if (hito.AcuerdoComercialViaId != pago.AcuerdoComercialViaId)
                    {
                        throw new InvalidOperationException("El hito no pertenece a la misma via comercial del pago.");
                    }

                    hito.ImporteAplicado += request.ImporteAplicado;
                    hito.Estado = hito.ImporteEstimado > 0 && hito.ImporteAplicado >= hito.ImporteEstimado
                        ? HitoEstado.Cumplido
                        : HitoEstado.Parcial;
                }
                else if (request.CuotaComercialId.HasValue || request.HitoComercialViaId.HasValue)
                {
                    throw new InvalidOperationException("La imputacion seleccionada no debe incluir cuota ni hito.");
                }

                pago.Aplicaciones.Add(new AplicacionPagoComercial
                {
                    PagoComercialId = pago.Id,
                    CuotaComercialId = cuota?.Id,
                    HitoComercialViaId = hito?.Id,
                    ImporteAplicado = request.ImporteAplicado,
                    FechaAplicacion = DateTime.UtcNow,
                    TipoImputacion = request.TipoImputacion,
                    Observaciones = request.Observaciones,
                    UsuarioAplicacion = _userContext.UserName
                });
            }

            pago.Estado = pago.Aplicaciones.Sum(x => x.ImporteAplicado) > 0 ? PagoEstado.Aplicado : PagoEstado.Registrado;
            await _db.SaveChangesAsync();
            return pago;
        }

        private static void UpdateCuotaEstado(CuotaComercial cuota)
        {
            if (cuota.Estado == CuotaEstado.Anulada) return;
            if (cuota.SaldoPendiente <= 0)
            {
                cuota.Estado = CuotaEstado.Pagada;
                return;
            }
            if (cuota.FechaVencimiento < DateTime.UtcNow && cuota.ImportePagado == 0)
            {
                cuota.Estado = CuotaEstado.Vencida;
                return;
            }
            cuota.Estado = cuota.ImportePagado > 0 ? CuotaEstado.Parcial : CuotaEstado.Pendiente;
        }

        private static string NormalizeCurrency(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "ARS" : value.Trim().ToUpperInvariant();
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private IQueryable<PagoComercial> GetPagoQuery()
        {
            return _db.PagosComerciales
                .Include(p => p.AcuerdoComercialVia)
                .Include(p => p.Aplicaciones)
                    .ThenInclude(a => a.HitoComercialVia)
                .Include(p => p.Aplicaciones)
                    .ThenInclude(a => a.CuotaComercial)
                        .ThenInclude(c => c!.PlanPago)
                            .ThenInclude(pp => pp.AcuerdoComercialVia);
        }

        private static PagoComercialResponse MapPago(PagoComercial pago)
        {
            return new PagoComercialResponse
            {
                Id = pago.Id,
                ClienteExternoId = pago.ClienteExternoId,
                ObraExternaId = pago.ObraExternaId,
                AcuerdoComercialId = pago.AcuerdoComercialId,
                AcuerdoComercialViaId = pago.AcuerdoComercialViaId,
                FechaPago = pago.FechaPago,
                MonedaCodigo = pago.MonedaCodigo,
                ImporteTotal = pago.ImporteTotal,
                MedioPago = pago.MedioPago,
                TipoImputacion = pago.TipoImputacion,
                OrigenPago = pago.OrigenPago,
                Observaciones = pago.Observaciones,
                Estado = pago.Estado,
                FechaAlta = pago.FechaAlta,
                UsuarioAlta = pago.UsuarioAlta,
                FechaAnulacion = pago.FechaAnulacion,
                UsuarioAnulacion = pago.UsuarioAnulacion,
                MotivoAnulacion = pago.MotivoAnulacion,
                Aplicaciones = pago.Aplicaciones.OrderBy(a => a.Id).Select(MapAplicacion).ToList()
            };
        }

        private static AplicacionPagoResponse MapAplicacion(AplicacionPagoComercial aplicacion)
        {
            return new AplicacionPagoResponse
            {
                Id = aplicacion.Id,
                PagoComercialId = aplicacion.PagoComercialId,
                CuotaComercialId = aplicacion.CuotaComercialId,
                HitoComercialViaId = aplicacion.HitoComercialViaId,
                ImporteAplicado = aplicacion.ImporteAplicado,
                FechaAplicacion = aplicacion.FechaAplicacion,
                TipoImputacion = aplicacion.TipoImputacion,
                Observaciones = aplicacion.Observaciones,
                UsuarioAplicacion = aplicacion.UsuarioAplicacion
            };
        }
    }
}

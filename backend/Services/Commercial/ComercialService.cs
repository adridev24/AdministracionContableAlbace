using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Commercial
{
    public class ComercialService : IComercialService
    {
        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;

        public ComercialService(AppDbContext db, IUserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        public async Task<AcuerdoResponse> CreateAcuerdoAsync(CreateAcuerdoRequest request)
        {
            var viasRequest = NormalizeViasRequest(request);
            var usuario = _userContext.UserName;

            var acuerdo = new AcuerdoComercial
            {
                ClienteExternoId = request.ClienteExternoId,
                ObraExternaId = request.ObraExternaId,
                NumeroAcuerdo = request.NumeroAcuerdo,
                FechaAcuerdo = EnsureUtc(request.FechaAcuerdo),
                Descripcion = request.Descripcion,
                MontoTotal = viasRequest.Sum(v => v.MontoActual ?? v.MontoOriginal),
                Estado = request.Estado,
                ViaOperacion = viasRequest.Count == 1 ? viasRequest[0].ViaOperacion : ViaOperacion.Via1,
                Observaciones = request.Observaciones,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = usuario
            };

            foreach (var viaRequest in viasRequest)
            {
                acuerdo.Vias.Add(BuildVia(acuerdo, viaRequest, usuario));
            }

            _db.AcuerdosComerciales.Add(acuerdo);
            await _db.SaveChangesAsync();

            return MapAcuerdo(acuerdo);
        }

        public async Task<AcuerdoDetalleResponse?> GetAcuerdoDetalleAsync(int id)
        {
            var acuerdo = await GetAcuerdoQuery()
                .FirstOrDefaultAsync(a => a.Id == id);

            return acuerdo == null ? null : MapDetalle(acuerdo);
        }

        public async Task<AcuerdoResponse> AprobarAcuerdoAsync(int acuerdoId)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.Vias)
                    .ThenInclude(v => v.PlanPago)
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("Solo los acuerdos en estado Borrador pueden aprobarse.");
            }

            var viasPlanificadasSinPlan = acuerdo.Vias
                .Where(v => v.Estado != AcuerdoEstado.Anulado && v.ModalidadCobro == ModalidadCobro.Planificada && v.PlanPago == null)
                .Select(v => v.ViaOperacion.ToString())
                .ToList();

            if (viasPlanificadasSinPlan.Any())
            {
                throw new InvalidOperationException($"No se puede aprobar el acuerdo. Cree el plan de pago para: {string.Join(", ", viasPlanificadasSinPlan)}.");
            }

            acuerdo.Estado = AcuerdoEstado.Aprobado;
            foreach (var via in acuerdo.Vias.Where(v => v.Estado == AcuerdoEstado.Borrador))
            {
                via.Estado = AcuerdoEstado.Aprobado;
            }

            await _db.SaveChangesAsync();
            return MapAcuerdo(acuerdo);
        }

        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorClienteAsync(string clienteExternoId)
        {
            var acuerdos = await GetAcuerdoQuery()
                .Where(a => a.ClienteExternoId == clienteExternoId)
                .OrderByDescending(a => a.FechaAlta)
                .ToListAsync();

            return acuerdos.Select(MapAcuerdo).ToList();
        }

        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorObraAsync(string obraExternaId)
        {
            var acuerdos = await GetAcuerdoQuery()
                .Where(a => a.ObraExternaId == obraExternaId)
                .OrderByDescending(a => a.FechaAlta)
                .ToListAsync();

            return acuerdos.Select(MapAcuerdo).ToList();
        }

        public async Task<AcuerdoViaResponse> CrearViaAsync(int acuerdoId, CreateAcuerdoViaRequest request)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.Vias)
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Vias.Any(v => v.ViaOperacion == request.ViaOperacion))
            {
                throw new InvalidOperationException("El acuerdo ya tiene una vía registrada con esa operación.");
            }

            var via = BuildVia(acuerdo, request, _userContext.UserName);
            acuerdo.Vias.Add(via);
            await RecalculateAcuerdoMontoTotalAsync(acuerdo);
            await _db.SaveChangesAsync();

            return MapVia(via);
        }

        public async Task<AcuerdoViaResponse> ModificarMontoViaAsync(int acuerdoViaId, ModificarMontoViaRequest request)
        {
            var via = await GetViaQuery()
                .FirstOrDefaultAsync(v => v.Id == acuerdoViaId);

            if (via == null)
            {
                throw new InvalidOperationException("Vía comercial no encontrada.");
            }

            if (via.Estado != AcuerdoEstado.Borrador || via.AcuerdoComercial.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("El monto de la via solo puede modificarse antes de aprobar el acuerdo.");
            }

            var totalPagado = GetTotalPagadoVia(via);
            if (request.NuevoMonto < totalPagado)
            {
                throw new InvalidOperationException("El nuevo monto de la vía no puede ser menor al total ya pagado.");
            }

            var montoAnterior = via.MontoActual;
            via.MontoActual = request.NuevoMonto;

            _db.AjustesAcuerdosComercialesVias.Add(new AjusteAcuerdoComercialVia
            {
                AcuerdoComercialViaId = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                ViaOperacion = via.ViaOperacion,
                MonedaCodigo = via.MonedaCodigo,
                MontoAnterior = montoAnterior,
                MontoNuevo = request.NuevoMonto,
                Diferencia = request.NuevoMonto - montoAnterior,
                TipoAjuste = request.RefinanciarCuotasPendientes ? TipoAjusteVia.RefinanciacionAutomatica : TipoAjusteVia.CambioMonto,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = _userContext.UserName
            });

            if (request.RefinanciarCuotasPendientes)
            {
                RefinanciarCuotasPendientes(via, request, _userContext.UserName);
            }

            ValidatePlanTotalMatchesVia(via);
            await RecalculateAcuerdoMontoTotalAsync(via.AcuerdoComercial);
            await _db.SaveChangesAsync();

            return MapVia(via);
        }

        public async Task<PlanPagoResponse> CrearPlanPagoAsync(int acuerdoViaId, CreatePlanPagoRequest request)
        {
            var via = await _db.AcuerdosComercialesVias
                .Include(v => v.PlanPago)
                .FirstOrDefaultAsync(v => v.Id == acuerdoViaId);

            if (via == null)
            {
                throw new InvalidOperationException("Vía comercial no encontrada.");
            }

            if (via.PlanPago != null)
            {
                throw new InvalidOperationException("La vía ya tiene un plan de pago asociado.");
            }

            if (via.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("El plan base solo puede crearse antes de aprobar el acuerdo.");
            }

            ValidatePlanRequest(via, request);

            var plan = new PlanPago
            {
                AcuerdoComercialViaId = acuerdoViaId,
                AcuerdoComercialId = via.AcuerdoComercialId,
                TieneAnticipo = request.TieneAnticipo,
                MontoAnticipo = request.MontoAnticipo,
                CantidadCuotas = request.CantidadCuotas,
                FechaPrimerVencimiento = EnsureUtc(request.FechaPrimerVencimiento),
                Periodicidad = request.Periodicidad,
                Observaciones = request.Observaciones
            };

            if (request.TieneAnticipo)
            {
                plan.Cuotas.Add(BuildCuota(0, TipoCuota.Anticipo, request.FechaPrimerVencimiento, request.MontoAnticipo));
            }

            var totalRemanente = via.MontoActual - request.MontoAnticipo;
            foreach (var cuota in BuildCuotas(request, totalRemanente))
            {
                plan.Cuotas.Add(cuota);
            }

            via.PlanPago = plan;
            ValidatePlanTotalMatchesVia(via);
            await _db.SaveChangesAsync();

            return MapPlanPago(plan);
        }

        public async Task<PlanPagoResponse> ActualizarPlanPagoAsync(int acuerdoViaId, UpdatePlanPagoRequest request)
        {
            var via = await GetViaQuery()
                .FirstOrDefaultAsync(v => v.Id == acuerdoViaId);

            if (via?.PlanPago == null)
            {
                throw new InvalidOperationException("La vía no tiene un plan de pago asociado.");
            }

            if (via.Estado != AcuerdoEstado.Borrador || via.AcuerdoComercial.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("El plan base solo puede modificarse antes de aprobar el acuerdo.");
            }

            var plan = via.PlanPago;
            ValidatePlanUpdateRequest(via, request);
            var cuotasById = plan.Cuotas.ToDictionary(c => c.Id);

            foreach (var cuotaRequest in request.Cuotas)
            {
                if (!cuotasById.TryGetValue(cuotaRequest.Id, out var cuota))
                {
                    throw new InvalidOperationException($"La cuota con id {cuotaRequest.Id} no se encuentra en el plan.");
                }

                if (cuota.Estado == CuotaEstado.Pagada)
                {
                    throw new InvalidOperationException("No se puede modificar una cuota pagada.");
                }

                if (cuota.ImportePagado > cuotaRequest.ImporteOriginal)
                {
                    throw new InvalidOperationException("El importe original no puede ser menor al importe ya pagado.");
                }

                cuota.FechaVencimiento = EnsureUtc(cuotaRequest.FechaVencimiento);
                cuota.ImporteOriginal = cuotaRequest.ImporteOriginal;
                cuota.SaldoPendiente = Math.Max(cuota.ImporteOriginal - cuota.ImportePagado, 0);
                UpdateCuotaEstado(cuota);
            }

            plan.TieneAnticipo = request.TieneAnticipo;
            plan.MontoAnticipo = request.MontoAnticipo;
            plan.CantidadCuotas = request.CantidadCuotas;
            plan.FechaPrimerVencimiento = EnsureUtc(request.FechaPrimerVencimiento);
            plan.Periodicidad = request.Periodicidad;
            plan.Observaciones = request.Observaciones;

            SyncAnticipoCuota(plan);
            ValidatePlanTotalMatchesVia(via);
            await _db.SaveChangesAsync();

            return MapPlanPago(plan);
        }

        public async Task<EstadoComercialResponse> GetEstadoComercialAsync(int acuerdoId)
        {
            var acuerdo = await GetAcuerdoQuery()
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            var totalPrometido = acuerdo.Vias.Where(IsViaActiva).Sum(GetMontoVigenteVia);
            var totalPagado = acuerdo.Vias.Sum(GetTotalPagadoVia);
            return new EstadoComercialResponse
            {
                AcuerdoComercialId = acuerdo.Id,
                TotalPrometido = totalPrometido,
                TotalPagado = totalPagado,
                SaldoRestante = Math.Max(totalPrometido - totalPagado, 0)
            };
        }

        public async Task<EstadoComercialResponse> GetEstadoComercialViaAsync(int acuerdoViaId)
        {
            var via = await GetViaQuery()
                .FirstOrDefaultAsync(v => v.Id == acuerdoViaId);

            if (via == null)
            {
                throw new InvalidOperationException("Vía comercial no encontrada.");
            }

            var totalPagado = GetTotalPagadoVia(via);
            return new EstadoComercialResponse
            {
                AcuerdoComercialId = via.AcuerdoComercialId,
                AcuerdoComercialViaId = via.Id,
                TotalPrometido = GetMontoVigenteVia(via),
                TotalPagado = totalPagado,
                SaldoRestante = Math.Max(GetMontoVigenteVia(via) - totalPagado, 0)
            };
        }

        public async Task<AcuerdoSituacionVia1Response> ObtenerSituacionVia1Async(int acuerdoId)
        {
            var via = await _db.AcuerdosComercialesVias
                .AsNoTracking()
                .Include(v => v.PlanPago)
                    .ThenInclude(p => p!.Cuotas)
                .FirstOrDefaultAsync(v => v.AcuerdoComercialId == acuerdoId && v.ViaOperacion == ViaOperacion.Via1);

            if (via == null)
            {
                throw new InvalidOperationException("El acuerdo no posee Vía 1.");
            }

            var obligacionesBase = via.PlanPago?.Cuotas
                .Where(c => c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.TipoCuota == TipoCuota.Anticipo ? 0 : 1)
                .ThenBy(c => c.FechaVencimiento)
                .ThenBy(c => c.NumeroCuota)
                .ToList() ?? new List<CuotaComercial>();

            var obligacionIds = obligacionesBase.Select(c => c.Id).ToList();
            var facturacion = await BuildSituacionFacturacionAsync(obligacionIds);
            var cobranzas = await BuildSituacionCobranzasAsync(obligacionIds);

            var obligaciones = obligacionesBase.Select(cuota =>
            {
                facturacion.TryGetValue(cuota.Id, out var facturado);
                cobranzas.TryGetValue(cuota.Id, out var cobrado);

                var previsto = RoundMoney(cuota.ImporteOriginal);
                var importeFacturado = RoundMoney(facturado.FacturadoConfirmado);
                var importeCobrado = RoundMoney(cobrado);
                var pendienteFacturar = RoundMoney(Math.Max(previsto - importeFacturado, 0));
                var facturadoPendienteCobro = RoundMoney(Math.Max(importeFacturado - importeCobrado, 0));
                var pendienteTotal = RoundMoney(Math.Max(previsto - importeCobrado, 0));

                return new AcuerdoSituacionVia1ObligacionResponse
                {
                    ObligacionId = cuota.Id,
                    Tipo = cuota.TipoCuota.ToString(),
                    Numero = cuota.NumeroCuota,
                    FechaVencimiento = cuota.FechaVencimiento,
                    ImportePrevisto = previsto,
                    ImporteFacturado = importeFacturado,
                    ImporteReservadoBorradores = RoundMoney(facturado.ReservadoBorrador),
                    PendienteFacturar = pendienteFacturar,
                    ImporteCobrado = importeCobrado,
                    FacturadoPendienteCobro = facturadoPendienteCobro,
                    PendienteTotal = pendienteTotal,
                    EstadoFacturacion = GetEstadoFacturacion(previsto, importeFacturado),
                    EstadoCobranza = GetEstadoCobranza(importeFacturado, importeCobrado)
                };
            }).ToList();

            var montoAcordado = obligaciones.Any()
                ? obligaciones.Sum(o => o.ImportePrevisto)
                : GetMontoVigenteVia(via);

            return new AcuerdoSituacionVia1Response
            {
                AcuerdoId = acuerdoId,
                ViaId = via.Id,
                MonedaCodigo = via.MonedaCodigo,
                MontoAcordado = RoundMoney(montoAcordado),
                TotalFacturado = RoundMoney(obligaciones.Sum(o => o.ImporteFacturado)),
                TotalReservadoBorradores = RoundMoney(obligaciones.Sum(o => o.ImporteReservadoBorradores)),
                PendienteFacturar = RoundMoney(obligaciones.Sum(o => o.PendienteFacturar)),
                TotalCobrado = RoundMoney(obligaciones.Sum(o => o.ImporteCobrado)),
                FacturadoPendienteCobro = RoundMoney(obligaciones.Sum(o => o.FacturadoPendienteCobro)),
                PendienteTotal = RoundMoney(obligaciones.Sum(o => o.PendienteTotal)),
                Obligaciones = obligaciones
            };
        }

        public async Task<IEnumerable<AcuerdoSituacionVia1FacturaResponse>> ObtenerFacturasSituacionVia1Async(int acuerdoId, int obligacionId)
        {
            await EnsureObligacionVia1Async(acuerdoId, obligacionId);

            var vinculaciones = await _db.VinculacionesFacturaComerciales
                .AsNoTracking()
                .Where(v => v.CuotaComercialId == obligacionId)
                .Select(v => new
                {
                    v.FacturaExternaId,
                    v.ImporteVinculado
                })
                .ToListAsync();

            var ventaIds = vinculaciones
                .Select(v => TryParseFacturaExternaId(v.FacturaExternaId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var ventas = await _db.Ventas
                .AsNoTracking()
                .Include(v => v.TipoComprobante)
                .Where(v => ventaIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            return vinculaciones
                .Select(v => new { Vinculacion = v, VentaId = TryParseFacturaExternaId(v.FacturaExternaId) })
                .Where(item => item.VentaId.HasValue && ventas.ContainsKey(item.VentaId.Value))
                .Select(item =>
                {
                    var venta = ventas[item.VentaId!.Value];
                    return new AcuerdoSituacionVia1FacturaResponse
                    {
                        VentaId = venta.Id,
                        Comprobante = BuildComprobante(venta),
                        Fecha = venta.FechaComprobante,
                        Estado = venta.Estado.ToString(),
                        TotalFactura = RoundMoney(venta.Total),
                        ImporteAplicadoObligacion = RoundMoney(item.Vinculacion.ImporteVinculado)
                    };
                })
                .OrderBy(item => item.Fecha)
                .ThenBy(item => item.VentaId)
                .ToList();
        }

        public async Task<IEnumerable<AcuerdoSituacionVia1CobranzaResponse>> ObtenerCobranzasSituacionVia1Async(int acuerdoId, int obligacionId)
        {
            await EnsureObligacionVia1Async(acuerdoId, obligacionId);

            var aplicaciones = await _db.CobranzasAplicacionesObligacion
                .AsNoTracking()
                .Include(a => a.AplicacionFactura)
                    .ThenInclude(a => a.Venta)
                        .ThenInclude(v => v.TipoComprobante)
                .Include(a => a.AplicacionFactura)
                    .ThenInclude(a => a.Cobranza)
                        .ThenInclude(c => c.MediosPago)
                            .ThenInclude(m => m.MedioPago)
                .Where(a => a.CuotaComercialId == obligacionId)
                .ToListAsync();

            return aplicaciones
                .Select(aplicacion =>
                {
                    var cobranza = aplicacion.AplicacionFactura.Cobranza;
                    var venta = aplicacion.AplicacionFactura.Venta;
                    return new AcuerdoSituacionVia1CobranzaResponse
                    {
                        CobranzaId = cobranza.Id,
                        Fecha = cobranza.Fecha,
                        Estado = cobranza.Estado.ToString(),
                        VentaId = venta.Id,
                        ComprobanteFactura = BuildComprobante(venta),
                        ImporteAplicadoObligacion = RoundMoney(aplicacion.ImporteAplicado),
                        MediosPago = BuildMediosPago(cobranza)
                    };
                })
                .OrderBy(item => item.Fecha)
                .ThenBy(item => item.CobranzaId)
                .ToList();
        }

        public async Task<SaldoComercialResponse> GetSaldoComercialClienteAsync(string clienteExternoId)
        {
            var acuerdos = await GetAcuerdoQuery()
                .Where(a => a.ClienteExternoId == clienteExternoId && a.Estado != AcuerdoEstado.Anulado)
                .ToListAsync();

            return MapSaldo(clienteExternoId, acuerdos);
        }

        public async Task<SaldoComercialResponse> GetSaldoComercialObraAsync(string obraExternaId)
        {
            var acuerdos = await GetAcuerdoQuery()
                .Where(a => a.ObraExternaId == obraExternaId && a.Estado != AcuerdoEstado.Anulado)
                .ToListAsync();

            return MapSaldo(obraExternaId, acuerdos);
        }

        public async Task<ReporteComercialResumenResponse> GetReporteComercialResumenAsync(DateTime periodoDesde, DateTime periodoHasta, ViaOperacion? viaOperacion = null)
        {
            var desde = DateTime.SpecifyKind(periodoDesde.Date, DateTimeKind.Utc);
            var hasta = DateTime.SpecifyKind(periodoHasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            var hoy = DateTime.UtcNow.Date;

            var acuerdosActivos = await GetAcuerdoQuery()
                .Where(a => a.Estado != AcuerdoEstado.Anulado && a.Estado != AcuerdoEstado.Finalizado)
                .ToListAsync();

            var viasActivas = acuerdosActivos
                .SelectMany(a => a.Vias)
                .Where(IsViaActiva)
                .Where(v => !viaOperacion.HasValue || v.ViaOperacion == viaOperacion.Value)
                .ToList();
            var acuerdosActivosFiltrados = acuerdosActivos
                .Where(a => a.Vias.Any(v => IsViaActiva(v) && (!viaOperacion.HasValue || v.ViaOperacion == viaOperacion.Value)))
                .ToList();
            var cuotasPendientes = viasActivas
                .SelectMany(v => v.PlanPago?.Cuotas ?? Enumerable.Empty<CuotaComercial>())
                .Where(c => c.SaldoPendiente > 0 && c.Estado != CuotaEstado.Pagada && c.Estado != CuotaEstado.Anulada)
                .ToList();

            cuotasPendientes.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();

            var pagosPeriodo = await _db.PagosComerciales
                .Include(p => p.AcuerdoComercialVia)
                .Where(p => p.Estado != PagoEstado.Anulado && p.FechaPago >= desde && p.FechaPago <= hasta)
                .ToListAsync();

            if (viaOperacion.HasValue)
            {
                pagosPeriodo = pagosPeriodo.Where(p => p.AcuerdoComercialVia != null && p.AcuerdoComercialVia.ViaOperacion == viaOperacion.Value).ToList();
            }

            var viaIds = viasActivas.Select(v => v.Id).ToList();
            var cobradoVia1PorVia = await BuildCobradoVia1PorViaAsync(viaIds);
            var cobradoVia1PeriodoPorVia = await BuildCobradoVia1PorViaAsync(viaIds, desde, hasta);

            var deudaPorCliente = acuerdosActivosFiltrados
                .SelectMany(a => a.Vias
                    .Where(IsViaActiva)
                    .Where(v => !viaOperacion.HasValue || v.ViaOperacion == viaOperacion.Value)
                    .Select(v => new { Acuerdo = a, Via = v }))
                .GroupBy(item => new { item.Acuerdo.ClienteExternoId, item.Via.MonedaCodigo })
                .Select(group =>
                {
                    var viasGrupo = group.Select(item => item.Via).ToList();
                    var totalAcordado = viasGrupo.Sum(GetMontoVigenteVia);
                    var totalPagado = viasGrupo.Sum(via => GetTotalCobradoReporteVia(via, cobradoVia1PorVia));
                    return new ClienteDeudaReporteResponse
                    {
                        ClienteExternoId = group.Key.ClienteExternoId,
                        MonedaCodigo = group.Key.MonedaCodigo,
                        TotalAcordado = totalAcordado,
                        TotalPagado = totalPagado,
                        SaldoPendiente = Math.Max(totalAcordado - totalPagado, 0),
                        AcuerdosActivos = group.Select(item => item.Acuerdo.Id).Distinct().Count()
                    };
                })
                .Where(item => item.SaldoPendiente > 0)
                .OrderBy(item => item.MonedaCodigo)
                .ThenByDescending(item => item.SaldoPendiente)
                .ToList();

            var cuotasPeriodo = cuotasPendientes.Where(c => c.FechaVencimiento >= desde && c.FechaVencimiento <= hasta).ToList();
            var cuotasVencidas = cuotasPendientes.Where(c => c.FechaVencimiento.Date < hoy).ToList();
            var monedas = viasActivas.Select(v => v.MonedaCodigo)
                .Concat(pagosPeriodo.Select(p => p.MonedaCodigo))
                .Concat(viasActivas.Where(v => cobradoVia1PeriodoPorVia.ContainsKey(v.Id)).Select(v => v.MonedaCodigo))
                .Concat(cuotasPendientes.Select(c => c.PlanPago.AcuerdoComercialVia.MonedaCodigo))
                .Concat(deudaPorCliente.Select(d => d.MonedaCodigo))
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();

            var totalesPorMoneda = monedas.Select(moneda => new ReporteComercialTotalMonedaResponse
            {
                MonedaCodigo = moneda,
                TotalAcordadoActivo = viasActivas
                    .Where(v => v.MonedaCodigo.Equals(moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(GetMontoVigenteVia),
                TotalCobradoPeriodo = pagosPeriodo
                    .Where(p => p.AcuerdoComercialVia?.ViaOperacion != ViaOperacion.Via1 &&
                        p.MonedaCodigo.Equals(moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.ImporteTotal),
                TotalPorCobrarPeriodo = cuotasPeriodo
                    .Where(c => c.PlanPago.AcuerdoComercialVia.MonedaCodigo.Equals(moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(c => c.SaldoPendiente),
                TotalVencido = cuotasVencidas
                    .Where(c => c.PlanPago.AcuerdoComercialVia.MonedaCodigo.Equals(moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(c => c.SaldoPendiente),
                SaldoTotalClientes = deudaPorCliente
                    .Where(d => d.MonedaCodigo.Equals(moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(d => d.SaldoPendiente)
            }).ToList();

            foreach (var total in totalesPorMoneda)
            {
                total.TotalCobradoPeriodo = RoundMoney(total.TotalCobradoPeriodo + viasActivas
                    .Where(v => v.ViaOperacion == ViaOperacion.Via1 &&
                        v.MonedaCodigo.Equals(total.MonedaCodigo, StringComparison.OrdinalIgnoreCase))
                    .Sum(v => cobradoVia1PeriodoPorVia.GetValueOrDefault(v.Id)));
            }

            var unicoTotal = totalesPorMoneda.Count == 1 ? totalesPorMoneda[0] : null;

            return new ReporteComercialResumenResponse
            {
                PeriodoDesde = desde,
                PeriodoHasta = hasta,
                TotalAcordadoActivo = unicoTotal?.TotalAcordadoActivo ?? 0,
                TotalCobradoPeriodo = unicoTotal?.TotalCobradoPeriodo ?? 0,
                TotalPorCobrarPeriodo = unicoTotal?.TotalPorCobrarPeriodo ?? 0,
                TotalVencido = unicoTotal?.TotalVencido ?? 0,
                SaldoTotalClientes = unicoTotal?.SaldoTotalClientes ?? 0,
                AcuerdosActivos = acuerdosActivosFiltrados.Count,
                CuotasPendientesPeriodo = cuotasPeriodo.Count,
                CuotasVencidas = cuotasVencidas.Count,
                TotalesPorMoneda = totalesPorMoneda,
                ClientesConDeuda = deudaPorCliente.Take(10).ToList(),
                ProximosVencimientos = cuotasPendientes
                    .Where(c => c.FechaVencimiento >= hoy)
                    .OrderBy(c => c.FechaVencimiento)
                    .Take(10)
                    .Select(MapCuotaReporte)
                    .ToList()
            };
        }

        public async Task<IEnumerable<CuotaResponse>> GetCuotasVencidasAsync()
        {
            var cuotas = await _db.CuotasComerciales
                .Where(c => c.SaldoPendiente > 0 && c.FechaVencimiento < DateTime.UtcNow && c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            cuotas.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();
            return cuotas.Select(MapCuota).ToList();
        }

        public async Task<IEnumerable<CuotaResponse>> GetCuotasPendientesAsync()
        {
            var cuotas = await _db.CuotasComerciales
                .Where(c => c.SaldoPendiente > 0 && c.Estado != CuotaEstado.Pagada && c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            cuotas.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();
            return cuotas.Select(MapCuota).ToList();
        }

        public async Task<CuotaResponse> AjustarCuotaAsync(int cuotaId, AjusteCuotaRequest request)
        {
            var cuota = await _db.CuotasComerciales
                .Include(c => c.PlanPago)
                    .ThenInclude(p => p.AcuerdoComercialVia)
                        .ThenInclude(v => v.AcuerdoComercial)
                .FirstOrDefaultAsync(c => c.Id == cuotaId);

            if (cuota == null)
            {
                throw new InvalidOperationException("La cuota comercial no fue encontrada.");
            }

            if (cuota.Estado == CuotaEstado.Pagada)
            {
                throw new InvalidOperationException("Una cuota pagada no puede ajustarse.");
            }

            if (!request.NuevoImporteOriginal.HasValue && !request.NuevaFechaVencimiento.HasValue)
            {
                throw new InvalidOperationException("Debe especificar un nuevo importe o una nueva fecha de vencimiento.");
            }

            var via = cuota.PlanPago.AcuerdoComercialVia;
            if (via.Estado != AcuerdoEstado.Borrador || via.AcuerdoComercial.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("Las cuotas existentes solo pueden modificarse antes de aprobar el acuerdo. Para acuerdos aprobados agregue una cuota adicional o de ajuste.");
            }

            var importeAnterior = cuota.ImporteOriginal;
            var fechaAnterior = cuota.FechaVencimiento;

            if (request.NuevoImporteOriginal.HasValue)
            {
                if (request.NuevoImporteOriginal.Value < cuota.ImportePagado)
                {
                    throw new InvalidOperationException("El nuevo importe original no puede ser menor al importe ya pagado.");
                }
                cuota.ImporteOriginal = request.NuevoImporteOriginal.Value;
            }

            if (request.NuevaFechaVencimiento.HasValue)
            {
                cuota.FechaVencimiento = EnsureUtc(request.NuevaFechaVencimiento.Value);
            }

            cuota.SaldoPendiente = Math.Max(cuota.ImporteOriginal - cuota.ImportePagado, 0);
            UpdateCuotaEstado(cuota);
            ValidatePlanDoesNotExceedVia(via);
            ValidatePlanTotalMatchesVia(via);

            _db.AjustesCuotaComerciales.Add(new AjusteCuotaComercial
            {
                CuotaComercialId = cuota.Id,
                PlanPagoId = cuota.PlanPagoId,
                AcuerdoComercialViaId = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                TipoAjuste = GetTipoAjuste(request),
                ImporteAnterior = importeAnterior,
                ImporteNuevo = cuota.ImporteOriginal,
                FechaVencimientoAnterior = fechaAnterior,
                FechaVencimientoNueva = cuota.FechaVencimiento,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = _userContext.UserName
            });

            await _db.SaveChangesAsync();
            return MapCuota(cuota);
        }

        public async Task<CuotaResponse> AgregarCuotaAjusteAsync(int planPagoId, AddCuotaAjusteRequest request)
        {
            var plan = await _db.PlanesPago
                .Include(p => p.AcuerdoComercialVia)
                    .ThenInclude(v => v.AcuerdoComercial)
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == planPagoId);

            if (plan == null)
            {
                throw new InvalidOperationException("El plan de pago no fue encontrado.");
            }

            var via = plan.AcuerdoComercialVia;
            if (via.Estado != AcuerdoEstado.Aprobado && via.Estado != AcuerdoEstado.EnCurso)
            {
                throw new InvalidOperationException("Solo se pueden agregar cuotas a vías aprobadas o en curso.");
            }

            var cuota = BuildCuota(
                plan.Cuotas.Any() ? plan.Cuotas.Max(c => c.NumeroCuota) + 1 : 1,
                request.TipoCuota,
                request.FechaVencimiento,
                request.ImporteOriginal);

            var montoAnterior = via.MontoActual;
            via.MontoActual += request.ImporteOriginal;
            plan.Cuotas.Add(cuota);
            plan.CantidadCuotas += 1;
            ValidatePlanDoesNotExceedVia(via);

            _db.AjustesAcuerdosComercialesVias.Add(new AjusteAcuerdoComercialVia
            {
                AcuerdoComercialViaId = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                ViaOperacion = via.ViaOperacion,
                MonedaCodigo = via.MonedaCodigo,
                MontoAnterior = montoAnterior,
                MontoNuevo = via.MontoActual,
                Diferencia = request.ImporteOriginal,
                TipoAjuste = TipoAjusteVia.CambioMonto,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = _userContext.UserName
            });

            _db.AjustesCuotaComerciales.Add(new AjusteCuotaComercial
            {
                CuotaComercial = cuota,
                PlanPagoId = planPagoId,
                AcuerdoComercialViaId = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                TipoAjuste = TipoAjuste.NuevaCuota,
                ImporteAnterior = 0,
                ImporteNuevo = request.ImporteOriginal,
                FechaVencimientoAnterior = null,
                FechaVencimientoNueva = cuota.FechaVencimiento,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = _userContext.UserName
            });

            await RecalculateAcuerdoMontoTotalAsync(via.AcuerdoComercial);
            await _db.SaveChangesAsync();
            return MapCuota(cuota);
        }

        public async Task<HitoComercialResponse> CrearHitoAsync(int acuerdoViaId, CreateHitoComercialRequest request)
        {
            var via = await _db.AcuerdosComercialesVias
                .Include(v => v.Hitos)
                .FirstOrDefaultAsync(v => v.Id == acuerdoViaId);

            if (via == null)
            {
                throw new InvalidOperationException("Via comercial no encontrada.");
            }

            if (via.ModalidadCobro != ModalidadCobro.Abierta)
            {
                throw new InvalidOperationException("Los hitos solo se registran en vias de modalidad abierta.");
            }

            var hito = new HitoComercialVia
            {
                AcuerdoComercialViaId = via.Id,
                Descripcion = request.Descripcion,
                ImporteEstimado = request.ImporteEstimado,
                FechaReferencia = EnsureUtc(request.FechaReferencia),
                ImporteAplicado = 0,
                Estado = HitoEstado.Pendiente,
                Observaciones = request.Observaciones,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            via.Hitos.Add(hito);
            await _db.SaveChangesAsync();
            return MapHito(hito);
        }

        public async Task<IEnumerable<HitoComercialResponse>> GetHitosPorViaAsync(int acuerdoViaId)
        {
            var hitos = await _db.HitosComercialesVias
                .Where(h => h.AcuerdoComercialViaId == acuerdoViaId)
                .OrderBy(h => h.FechaReferencia)
                .ToListAsync();

            return hitos.Select(MapHito).ToList();
        }

        public async Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorCuotaAsync(int cuotaId)
        {
            var ajustes = await _db.AjustesCuotaComerciales
                .Where(a => a.CuotaComercialId == cuotaId)
                .OrderByDescending(a => a.FechaAjuste)
                .ToListAsync();

            return ajustes.Select(MapAjusteCuota).ToList();
        }

        public async Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorAcuerdoAsync(int acuerdoId)
        {
            var ajustes = await _db.AjustesCuotaComerciales
                .Where(a => a.AcuerdoComercialId == acuerdoId)
                .OrderByDescending(a => a.FechaAjuste)
                .ToListAsync();

            return ajustes.Select(MapAjusteCuota).ToList();
        }

        public async Task<IEnumerable<AjusteAcuerdoViaResponse>> GetHistorialAjustesPorViaAsync(int acuerdoViaId)
        {
            var ajustes = await _db.AjustesAcuerdosComercialesVias
                .Where(a => a.AcuerdoComercialViaId == acuerdoViaId)
                .OrderByDescending(a => a.FechaAjuste)
                .ToListAsync();

            return ajustes.Select(MapAjusteVia).ToList();
        }

        private IQueryable<AcuerdoComercial> GetAcuerdoQuery()
        {
            return _db.AcuerdosComerciales
                .Include(a => a.Vias)
                    .ThenInclude(v => v.PlanPago)
                        .ThenInclude(p => p!.Cuotas)
                .Include(a => a.Vias)
                    .ThenInclude(v => v.Pagos)
                        .ThenInclude(p => p.Aplicaciones)
                .Include(a => a.Vias)
                    .ThenInclude(v => v.Hitos)
                .Include(a => a.Vias)
                    .ThenInclude(v => v.Ajustes)
                .AsSplitQuery();
        }

        private IQueryable<AcuerdoComercialVia> GetViaQuery()
        {
            return _db.AcuerdosComercialesVias
                .Include(v => v.AcuerdoComercial)
                .Include(v => v.PlanPago)
                    .ThenInclude(p => p!.Cuotas)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .Include(v => v.Hitos)
                .Include(v => v.Ajustes)
                .AsSplitQuery();
        }

        private static List<CreateAcuerdoViaRequest> NormalizeViasRequest(CreateAcuerdoRequest request)
        {
            var vias = request.Vias.Where(v => v.MontoOriginal > 0).ToList();
            if (!vias.Any() && request.MontoTotal.HasValue && request.MontoTotal.Value > 0)
            {
                vias.Add(new CreateAcuerdoViaRequest
                {
                    ViaOperacion = request.ViaOperacion ?? ViaOperacion.Via1,
                    MonedaCodigo = "ARS",
                    MontoOriginal = request.MontoTotal.Value,
                    MontoActual = request.MontoTotal.Value,
                    Estado = request.Estado,
                    ModalidadCobro = request.ViaOperacion == ViaOperacion.Via2 ? ModalidadCobro.Abierta : ModalidadCobro.Planificada,
                    Observaciones = request.Observaciones
                });
            }

            if (!vias.Any())
            {
                throw new InvalidOperationException("Debe registrar al menos una vía con monto mayor a cero.");
            }

            if (vias.GroupBy(v => v.ViaOperacion).Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("No puede registrar dos vías iguales dentro del mismo acuerdo.");
            }

            return vias;
        }

        private static AcuerdoComercialVia BuildVia(AcuerdoComercial acuerdo, CreateAcuerdoViaRequest request, string usuarioDefault)
        {
            var montoActual = request.MontoActual ?? request.MontoOriginal;
            if (montoActual <= 0 || request.MontoOriginal <= 0)
            {
                throw new InvalidOperationException("El monto de la vía debe ser mayor a cero.");
            }

            return new AcuerdoComercialVia
            {
                AcuerdoComercial = acuerdo,
                ViaOperacion = request.ViaOperacion,
                ModalidadCobro = request.ModalidadCobro ?? (request.ViaOperacion == ViaOperacion.Via2 ? ModalidadCobro.Abierta : ModalidadCobro.Planificada),
                MonedaCodigo = NormalizeCurrency(request.MonedaCodigo),
                MontoOriginal = request.MontoOriginal,
                MontoActual = montoActual,
                Estado = request.Estado,
                Observaciones = request.Observaciones,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = usuarioDefault
            };
        }

        private static void ValidatePlanRequest(AcuerdoComercialVia via, CreatePlanPagoRequest request)
        {
            if (request.CantidadCuotas <= 0)
            {
                throw new InvalidOperationException("La cantidad de cuotas debe ser mayor a cero.");
            }

            if (request.TieneAnticipo && request.MontoAnticipo <= 0)
            {
                throw new InvalidOperationException("El monto de anticipo debe ser mayor a cero cuando hay anticipo.");
            }

            if (!request.TieneAnticipo && request.MontoAnticipo != 0)
            {
                throw new InvalidOperationException("No puede enviar un monto de anticipo cuando no tiene anticipo.");
            }

            if (request.MontoAnticipo >= via.MontoActual)
            {
                throw new InvalidOperationException("El anticipo debe ser menor al monto vigente de la vía.");
            }
        }

        private static void ValidatePlanUpdateRequest(AcuerdoComercialVia via, UpdatePlanPagoRequest request)
        {
            if (request.CantidadCuotas <= 0)
            {
                throw new InvalidOperationException("La cantidad de cuotas debe ser mayor a cero.");
            }

            if (request.TieneAnticipo && request.MontoAnticipo <= 0)
            {
                throw new InvalidOperationException("El monto de anticipo debe ser mayor a cero cuando hay anticipo.");
            }

            if (!request.TieneAnticipo && request.MontoAnticipo != 0)
            {
                throw new InvalidOperationException("No puede enviar un monto de anticipo cuando no tiene anticipo.");
            }

            if (request.MontoAnticipo >= via.MontoActual)
            {
                throw new InvalidOperationException("El anticipo debe ser menor al monto vigente de la via.");
            }
        }

        private static void ValidatePlanDoesNotExceedVia(AcuerdoComercialVia via)
        {
            var totalPlan = via.PlanPago?.Cuotas
                .Where(c => c.Estado != CuotaEstado.Anulada && (c.TipoCuota == TipoCuota.Anticipo || c.TipoCuota == TipoCuota.Cuota))
                .Sum(c => c.ImporteOriginal) ?? 0;

            if (totalPlan > via.MontoActual)
            {
                throw new InvalidOperationException("El total del plan de pago de la vía supera el monto vigente del acuerdo.");
            }
        }

        private async Task RecalculateAcuerdoMontoTotalAsync(AcuerdoComercial acuerdo)
        {
            acuerdo.MontoTotal = acuerdo.Vias.Where(IsViaActiva).Sum(GetMontoVigenteVia);
            await Task.CompletedTask;
        }

        private static void ValidatePlanTotalMatchesVia(AcuerdoComercialVia via)
        {
            if (via.PlanPago == null) return;

            var totalPlan = GetPlanTotal(via.PlanPago);
            if (Math.Abs(totalPlan - via.MontoActual) > 0.01m)
            {
                throw new InvalidOperationException("El total del plan no coincide con el monto de la via.");
            }
        }

        private static decimal GetPlanTotal(PlanPago? plan)
        {
            if (plan == null) return 0;

            var anticipo = plan.TieneAnticipo ? plan.MontoAnticipo : 0;
            var cuotas = plan.Cuotas
                .Where(c => c.Estado != CuotaEstado.Anulada && c.TipoCuota != TipoCuota.Anticipo)
                .Sum(c => c.ImporteOriginal);

            return anticipo + cuotas;
        }

        private static decimal GetMontoVigenteVia(AcuerdoComercialVia via)
        {
            return Math.Max(via.MontoActual, GetPlanTotal(via.PlanPago));
        }

        private static void SyncAnticipoCuota(PlanPago plan)
        {
            var anticipoCuota = plan.Cuotas.FirstOrDefault(c => c.TipoCuota == TipoCuota.Anticipo);

            if (plan.TieneAnticipo)
            {
                if (anticipoCuota == null)
                {
                    plan.Cuotas.Add(BuildCuota(0, TipoCuota.Anticipo, plan.FechaPrimerVencimiento, plan.MontoAnticipo));
                    return;
                }

                if (anticipoCuota.ImportePagado > plan.MontoAnticipo)
                {
                    throw new InvalidOperationException("El anticipo no puede ser menor al importe ya pagado.");
                }

                anticipoCuota.ImporteOriginal = plan.MontoAnticipo;
                anticipoCuota.SaldoPendiente = Math.Max(plan.MontoAnticipo - anticipoCuota.ImportePagado, 0);
                UpdateCuotaEstado(anticipoCuota);
                return;
            }

            if (anticipoCuota == null) return;

            if (anticipoCuota.ImportePagado > 0)
            {
                throw new InvalidOperationException("No se puede quitar un anticipo con pagos registrados.");
            }

            anticipoCuota.ImporteOriginal = 0;
            anticipoCuota.SaldoPendiente = 0;
            anticipoCuota.Estado = CuotaEstado.Anulada;
        }

        private static void RefinanciarCuotasPendientes(AcuerdoComercialVia via, ModificarMontoViaRequest request, string usuarioAjuste)
        {
            if (via.PlanPago == null) return;

            var cuotasNoPagadas = via.PlanPago.Cuotas
                .Where(c => c.TipoCuota != TipoCuota.Anticipo && c.Estado != CuotaEstado.Pagada && c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.FechaVencimiento)
                .ToList();

            if (!cuotasNoPagadas.Any()) return;

            var anticipo = via.PlanPago.TieneAnticipo ? via.PlanPago.MontoAnticipo : 0;
            var cuotasFijas = via.PlanPago.Cuotas
                .Where(c => c.TipoCuota != TipoCuota.Anticipo && !cuotasNoPagadas.Contains(c) && c.Estado != CuotaEstado.Anulada)
                .Sum(c => c.ImporteOriginal);
            var nuevoSaldoAFinanciar = via.MontoActual - anticipo - cuotasFijas;

            if (nuevoSaldoAFinanciar < 0)
            {
                throw new InvalidOperationException("El nuevo monto de la via no alcanza para cubrir el anticipo y las cuotas ya consolidadas.");
            }

            if (nuevoSaldoAFinanciar <= 0)
            {
                foreach (var cuota in cuotasNoPagadas)
                {
                    var nuevoImporte = cuota.ImportePagado;
                    AddAjusteCuota(via, cuota, cuota.ImporteOriginal, nuevoImporte, cuota.FechaVencimiento, request, usuarioAjuste);
                    cuota.ImporteOriginal = cuota.ImportePagado;
                    cuota.SaldoPendiente = 0;
                    cuota.Estado = cuota.ImportePagado > 0 ? CuotaEstado.Pagada : CuotaEstado.Anulada;
                }
                return;
            }

            var baseImporte = Math.Round(nuevoSaldoAFinanciar / cuotasNoPagadas.Count, 2);
            var asignado = baseImporte * cuotasNoPagadas.Count;
            var diferencia = nuevoSaldoAFinanciar - asignado;

            for (var i = 0; i < cuotasNoPagadas.Count; i++)
            {
                var cuota = cuotasNoPagadas[i];
                var nuevoImporte = baseImporte + (i == cuotasNoPagadas.Count - 1 ? diferencia : 0);
                nuevoImporte = Math.Max(nuevoImporte, cuota.ImportePagado);

                AddAjusteCuota(via, cuota, cuota.ImporteOriginal, nuevoImporte, cuota.FechaVencimiento, request, usuarioAjuste);
                cuota.ImporteOriginal = nuevoImporte;
                cuota.SaldoPendiente = Math.Max(cuota.ImporteOriginal - cuota.ImportePagado, 0);
                UpdateCuotaEstado(cuota);
            }
        }

        private static void AddAjusteCuota(AcuerdoComercialVia via, CuotaComercial cuota, decimal importeAnterior, decimal importeNuevo, DateTime fechaAnterior, ModificarMontoViaRequest request, string usuarioAjuste)
        {
            cuota.Ajustes.Add(new AjusteCuotaComercial
            {
                CuotaComercial = cuota,
                PlanPagoId = cuota.PlanPagoId,
                AcuerdoComercialViaId = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                TipoAjuste = TipoAjuste.CambioImporte,
                ImporteAnterior = importeAnterior,
                ImporteNuevo = importeNuevo,
                FechaVencimientoAnterior = fechaAnterior,
                FechaVencimientoNueva = cuota.FechaVencimiento,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = usuarioAjuste
            });
        }

        private static IEnumerable<CuotaComercial> BuildCuotas(CreatePlanPagoRequest request, decimal totalRemanente)
        {
            var cuotas = new List<CuotaComercial>();
            var baseCuota = Math.Round(totalRemanente / request.CantidadCuotas, 2);
            var asignado = baseCuota * request.CantidadCuotas;
            var diferencia = totalRemanente - asignado;

            for (var i = 1; i <= request.CantidadCuotas; i++)
            {
                var importe = baseCuota + (i == request.CantidadCuotas ? diferencia : 0);
                cuotas.Add(BuildCuota(i, TipoCuota.Cuota, GetFechaVencimiento(request.FechaPrimerVencimiento, request.Periodicidad, i - 1), importe));
            }

            return cuotas;
        }

        private static CuotaComercial BuildCuota(int numero, TipoCuota tipo, DateTime vencimiento, decimal importe)
        {
            return new CuotaComercial
            {
                NumeroCuota = numero,
                TipoCuota = tipo,
                FechaVencimiento = EnsureUtc(vencimiento),
                ImporteOriginal = importe,
                ImportePagado = 0,
                SaldoPendiente = importe,
                Estado = EnsureUtc(vencimiento) < DateTime.UtcNow ? CuotaEstado.Vencida : CuotaEstado.Pendiente
            };
        }

        private static DateTime GetFechaVencimiento(DateTime baseDate, string periodicidad, int offset)
        {
            var normalized = periodicidad?.Trim().ToLowerInvariant() ?? string.Empty;
            var utcBase = EnsureUtc(baseDate);
            return normalized switch
            {
                "quincenal" => utcBase.AddDays(15 * offset),
                "semanal" => utcBase.AddDays(7 * offset),
                "anual" => utcBase.AddYears(offset),
                _ => utcBase.AddMonths(offset)
            };
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

        private static TipoAjuste GetTipoAjuste(AjusteCuotaRequest request)
        {
            return request.NuevoImporteOriginal.HasValue && request.NuevaFechaVencimiento.HasValue
                ? TipoAjuste.CambioImporteYVencimiento
                : request.NuevoImporteOriginal.HasValue
                    ? TipoAjuste.CambioImporte
                    : TipoAjuste.CambioVencimiento;
        }

        private static bool IsViaActiva(AcuerdoComercialVia via)
        {
            return via.Estado != AcuerdoEstado.Anulado && via.Estado != AcuerdoEstado.Finalizado;
        }

        private static decimal GetTotalPagadoVia(AcuerdoComercialVia via)
        {
            return via.Pagos
                .Where(p => p.Estado != PagoEstado.Anulado)
                .Sum(p => p.ImporteTotal);
        }

        private static string NormalizeCurrency(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "ARS" : value.Trim().ToUpperInvariant();
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private async Task<Dictionary<int, SituacionFacturacionBalance>> BuildSituacionFacturacionAsync(IEnumerable<int> obligacionIds)
        {
            var ids = obligacionIds.Distinct().ToList();
            if (!ids.Any()) return new Dictionary<int, SituacionFacturacionBalance>();

            var vinculaciones = await _db.VinculacionesFacturaComerciales
                .AsNoTracking()
                .Where(v => ids.Contains(v.CuotaComercialId))
                .Select(v => new
                {
                    v.CuotaComercialId,
                    v.FacturaExternaId,
                    v.ImporteVinculado
                })
                .ToListAsync();

            var ventaIds = vinculaciones
                .Select(v => TryParseFacturaExternaId(v.FacturaExternaId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var estados = await _db.Ventas
                .AsNoTracking()
                .Where(v => ventaIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Estado })
                .ToDictionaryAsync(v => v.Id, v => v.Estado);

            var result = ids.ToDictionary(id => id, _ => SituacionFacturacionBalance.Empty);
            foreach (var vinculacion in vinculaciones)
            {
                var ventaId = TryParseFacturaExternaId(vinculacion.FacturaExternaId);
                if (!ventaId.HasValue || !estados.TryGetValue(ventaId.Value, out var estado)) continue;

                var current = result.GetValueOrDefault(vinculacion.CuotaComercialId, SituacionFacturacionBalance.Empty);
                var importe = RoundMoney(vinculacion.ImporteVinculado);
                if (estado == VentaEstado.Confirmada)
                {
                    current = current with { FacturadoConfirmado = RoundMoney(current.FacturadoConfirmado + importe) };
                }
                else if (estado == VentaEstado.Borrador)
                {
                    current = current with { ReservadoBorrador = RoundMoney(current.ReservadoBorrador + importe) };
                }

                result[vinculacion.CuotaComercialId] = current;
            }

            return result;
        }

        private async Task<Dictionary<int, decimal>> BuildSituacionCobranzasAsync(IEnumerable<int> obligacionIds)
        {
            var ids = obligacionIds.Distinct().ToList();
            if (!ids.Any()) return new Dictionary<int, decimal>();

            var cobranzas = await _db.CobranzasAplicacionesObligacion
                .AsNoTracking()
                .Where(a => ids.Contains(a.CuotaComercialId) &&
                    a.AplicacionFactura.Cobranza.Estado == CobranzaEstado.Confirmada)
                .GroupBy(a => a.CuotaComercialId)
                .Select(g => new { CuotaId = g.Key, Importe = g.Sum(x => x.ImporteAplicado) })
                .ToListAsync();

            return cobranzas.ToDictionary(g => g.CuotaId, g => RoundMoney(g.Importe));
        }

        private async Task<Dictionary<int, decimal>> BuildCobradoVia1PorViaAsync(IEnumerable<int> acuerdoViaIds, DateTime? desde = null, DateTime? hasta = null)
        {
            var ids = acuerdoViaIds.Distinct().ToList();
            if (!ids.Any()) return new Dictionary<int, decimal>();

            var query = _db.CobranzasAplicacionesObligacion
                .AsNoTracking()
                .Where(a => ids.Contains(a.CuotaComercial.PlanPago.AcuerdoComercialViaId) &&
                    a.CuotaComercial.PlanPago.AcuerdoComercialVia.ViaOperacion == ViaOperacion.Via1 &&
                    a.AplicacionFactura.Cobranza.Estado == CobranzaEstado.Confirmada);

            if (desde.HasValue)
            {
                query = query.Where(a => a.AplicacionFactura.Cobranza.Fecha >= desde.Value);
            }

            if (hasta.HasValue)
            {
                query = query.Where(a => a.AplicacionFactura.Cobranza.Fecha <= hasta.Value);
            }

            var cobranzas = await query
                .GroupBy(a => a.CuotaComercial.PlanPago.AcuerdoComercialViaId)
                .Select(g => new { AcuerdoViaId = g.Key, Importe = g.Sum(x => x.ImporteAplicado) })
                .ToListAsync();

            return cobranzas.ToDictionary(g => g.AcuerdoViaId, g => RoundMoney(g.Importe));
        }

        private static decimal GetTotalCobradoReporteVia(AcuerdoComercialVia via, IReadOnlyDictionary<int, decimal> cobradoVia1PorVia)
        {
            return via.ViaOperacion == ViaOperacion.Via1
                ? cobradoVia1PorVia.GetValueOrDefault(via.Id)
                : GetTotalPagadoVia(via);
        }

        private async Task EnsureObligacionVia1Async(int acuerdoId, int obligacionId)
        {
            var exists = await _db.CuotasComerciales
                .AsNoTracking()
                .AnyAsync(c => c.Id == obligacionId &&
                    c.PlanPago.AcuerdoComercialVia.AcuerdoComercialId == acuerdoId &&
                    c.PlanPago.AcuerdoComercialVia.ViaOperacion == ViaOperacion.Via1 &&
                    c.Estado != CuotaEstado.Anulada);

            if (!exists)
            {
                throw new InvalidOperationException("La obligación no pertenece a la Vía 1 del acuerdo.");
            }
        }

        private static int? TryParseFacturaExternaId(string facturaExternaId)
        {
            return int.TryParse(facturaExternaId, out var ventaId) ? ventaId : null;
        }

        private static string GetEstadoFacturacion(decimal previsto, decimal facturado)
        {
            if (facturado <= 0) return "SIN_FACTURAR";
            return facturado < previsto ? "PARCIALMENTE_FACTURADA" : "FACTURADA";
        }

        private static string GetEstadoCobranza(decimal facturado, decimal cobrado)
        {
            if (cobrado <= 0 || facturado <= 0) return "SIN_COBRAR";
            return cobrado < facturado ? "PARCIALMENTE_COBRADA" : "COBRADA";
        }

        private static string BuildComprobante(Venta venta)
        {
            var tipo = venta.TipoComprobante?.Codigo ?? "Factura";
            return $"{tipo} {venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}";
        }

        private static string? BuildMediosPago(Cobranza cobranza)
        {
            var medios = cobranza.MediosPago
                .OrderBy(m => m.Id)
                .Select(m =>
                {
                    var nombre = m.MedioPago?.Descripcion ?? "Medio";
                    var banco = string.IsNullOrWhiteSpace(m.Banco) ? null : m.Banco.Trim();
                    var referencia = string.IsNullOrWhiteSpace(m.NumeroReferencia) ? null : m.NumeroReferencia.Trim();
                    var partes = new[] { nombre, banco, referencia }.Where(p => !string.IsNullOrWhiteSpace(p));
                    return $"{string.Join(" / ", partes)} ({RoundMoney(m.Importe):N2})";
                })
                .ToList();

            return medios.Any() ? string.Join("; ", medios) : null;
        }

        private static AcuerdoDetalleResponse MapDetalle(AcuerdoComercial acuerdo)
        {
            var response = new AcuerdoDetalleResponse
            {
                Id = acuerdo.Id,
                ClienteExternoId = acuerdo.ClienteExternoId,
                ObraExternaId = acuerdo.ObraExternaId,
                NumeroAcuerdo = acuerdo.NumeroAcuerdo,
                FechaAcuerdo = acuerdo.FechaAcuerdo,
                Descripcion = acuerdo.Descripcion,
                MontoTotal = acuerdo.Vias.Where(IsViaActiva).Sum(GetMontoVigenteVia),
                Estado = acuerdo.Estado,
                ViaOperacion = acuerdo.Vias.Count == 1 ? acuerdo.Vias.First().ViaOperacion : null,
                Observaciones = acuerdo.Observaciones,
                FechaAlta = acuerdo.FechaAlta,
                UsuarioAlta = acuerdo.UsuarioAlta,
                Vias = acuerdo.Vias.OrderBy(v => v.ViaOperacion).Select(MapVia).ToList()
            };
            response.PlanPago = response.Vias.Count == 1 ? response.Vias[0].PlanPago : null;
            response.Pagos = response.Vias.SelectMany(v => v.Pagos).ToList();
            return response;
        }

        private static AcuerdoResponse MapAcuerdo(AcuerdoComercial acuerdo)
        {
            var vias = acuerdo.Vias.OrderBy(v => v.ViaOperacion).Select(MapVia).ToList();
            return new AcuerdoResponse
            {
                Id = acuerdo.Id,
                ClienteExternoId = acuerdo.ClienteExternoId,
                ObraExternaId = acuerdo.ObraExternaId,
                NumeroAcuerdo = acuerdo.NumeroAcuerdo,
                FechaAcuerdo = acuerdo.FechaAcuerdo,
                Descripcion = acuerdo.Descripcion,
                MontoTotal = vias.Where(v => v.Estado != AcuerdoEstado.Anulado && v.Estado != AcuerdoEstado.Finalizado).Sum(v => v.MontoActual),
                Estado = acuerdo.Estado,
                ViaOperacion = vias.Count == 1 ? vias.First().ViaOperacion : null,
                Observaciones = acuerdo.Observaciones,
                FechaAlta = acuerdo.FechaAlta,
                UsuarioAlta = acuerdo.UsuarioAlta,
                Vias = vias
            };
        }

        private static AcuerdoViaResponse MapVia(AcuerdoComercialVia via)
        {
            var totalPagado = GetTotalPagadoVia(via);
            var montoVigente = GetMontoVigenteVia(via);
            return new AcuerdoViaResponse
            {
                Id = via.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                ViaOperacion = via.ViaOperacion,
                ModalidadCobro = via.ModalidadCobro,
                MonedaCodigo = via.MonedaCodigo,
                MontoOriginal = via.MontoOriginal,
                MontoActual = montoVigente,
                Estado = via.Estado,
                Observaciones = via.Observaciones,
                FechaAlta = via.FechaAlta,
                UsuarioAlta = via.UsuarioAlta,
                TotalPagado = totalPagado,
                SaldoPendiente = Math.Max(montoVigente - totalPagado, 0),
                PlanPago = via.PlanPago == null ? null : MapPlanPago(via.PlanPago),
                Pagos = via.Pagos.OrderByDescending(p => p.FechaPago).Select(MapPago).ToList(),
                Hitos = via.Hitos.OrderBy(h => h.FechaReferencia).Select(MapHito).ToList(),
                Ajustes = via.Ajustes.OrderByDescending(a => a.FechaAjuste).Select(MapAjusteVia).ToList()
            };
        }

        private static PlanPagoResponse MapPlanPago(PlanPago plan)
        {
            return new PlanPagoResponse
            {
                Id = plan.Id,
                AcuerdoComercialId = plan.AcuerdoComercialId,
                AcuerdoComercialViaId = plan.AcuerdoComercialViaId,
                TieneAnticipo = plan.TieneAnticipo,
                MontoAnticipo = plan.MontoAnticipo,
                CantidadCuotas = plan.CantidadCuotas,
                FechaPrimerVencimiento = plan.FechaPrimerVencimiento,
                Periodicidad = plan.Periodicidad,
                Observaciones = plan.Observaciones,
                Cuotas = plan.Cuotas.OrderBy(c => c.NumeroCuota).Select(MapCuota).ToList()
            };
        }

        private static CuotaResponse MapCuota(CuotaComercial cuota)
        {
            return new CuotaResponse
            {
                Id = cuota.Id,
                PlanPagoId = cuota.PlanPagoId,
                NumeroCuota = cuota.NumeroCuota,
                TipoCuota = cuota.TipoCuota,
                FechaVencimiento = cuota.FechaVencimiento,
                ImporteOriginal = cuota.ImporteOriginal,
                ImportePagado = cuota.ImportePagado,
                SaldoPendiente = cuota.SaldoPendiente,
                Estado = cuota.Estado
            };
        }

        private static CuotaReporteResponse MapCuotaReporte(CuotaComercial cuota)
        {
            var via = cuota.PlanPago.AcuerdoComercialVia;
            var acuerdo = via.AcuerdoComercial;
            return new CuotaReporteResponse
            {
                CuotaId = cuota.Id,
                AcuerdoComercialId = via.AcuerdoComercialId,
                AcuerdoComercialViaId = via.Id,
                NumeroAcuerdo = acuerdo.NumeroAcuerdo,
                ClienteExternoId = acuerdo.ClienteExternoId,
                ObraExternaId = acuerdo.ObraExternaId,
                ViaOperacion = via.ViaOperacion,
                MonedaCodigo = via.MonedaCodigo,
                FechaVencimiento = cuota.FechaVencimiento,
                SaldoPendiente = cuota.SaldoPendiente,
                Estado = cuota.Estado
            };
        }

        private static AjusteCuotaResponse MapAjusteCuota(AjusteCuotaComercial ajuste)
        {
            return new AjusteCuotaResponse
            {
                Id = ajuste.Id,
                CuotaComercialId = ajuste.CuotaComercialId,
                PlanPagoId = ajuste.PlanPagoId,
                AcuerdoComercialViaId = ajuste.AcuerdoComercialViaId,
                AcuerdoComercialId = ajuste.AcuerdoComercialId,
                TipoAjuste = ajuste.TipoAjuste,
                ImporteAnterior = ajuste.ImporteAnterior,
                ImporteNuevo = ajuste.ImporteNuevo,
                FechaVencimientoAnterior = ajuste.FechaVencimientoAnterior,
                FechaVencimientoNueva = ajuste.FechaVencimientoNueva,
                Motivo = ajuste.Motivo,
                FechaAjuste = ajuste.FechaAjuste,
                UsuarioAjuste = ajuste.UsuarioAjuste
            };
        }

        private static AjusteAcuerdoViaResponse MapAjusteVia(AjusteAcuerdoComercialVia ajuste)
        {
            return new AjusteAcuerdoViaResponse
            {
                Id = ajuste.Id,
                AcuerdoComercialViaId = ajuste.AcuerdoComercialViaId,
                AcuerdoComercialId = ajuste.AcuerdoComercialId,
                ViaOperacion = ajuste.ViaOperacion,
                MonedaCodigo = ajuste.MonedaCodigo,
                MontoAnterior = ajuste.MontoAnterior,
                MontoNuevo = ajuste.MontoNuevo,
                Diferencia = ajuste.Diferencia,
                TipoAjuste = ajuste.TipoAjuste,
                Motivo = ajuste.Motivo,
                FechaAjuste = ajuste.FechaAjuste,
                UsuarioAjuste = ajuste.UsuarioAjuste
            };
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
                Aplicaciones = pago.Aplicaciones.OrderBy(a => a.Id).Select(a => new AplicacionPagoResponse
                {
                    Id = a.Id,
                    PagoComercialId = a.PagoComercialId,
                    CuotaComercialId = a.CuotaComercialId,
                    HitoComercialViaId = a.HitoComercialViaId,
                    ImporteAplicado = a.ImporteAplicado,
                    FechaAplicacion = a.FechaAplicacion,
                    TipoImputacion = a.TipoImputacion,
                    Observaciones = a.Observaciones,
                    UsuarioAplicacion = a.UsuarioAplicacion
                }).ToList()
            };
        }

        private static HitoComercialResponse MapHito(HitoComercialVia hito)
        {
            return new HitoComercialResponse
            {
                Id = hito.Id,
                AcuerdoComercialViaId = hito.AcuerdoComercialViaId,
                Descripcion = hito.Descripcion,
                ImporteEstimado = hito.ImporteEstimado,
                FechaReferencia = hito.FechaReferencia,
                ImporteAplicado = hito.ImporteAplicado,
                Estado = hito.Estado,
                Observaciones = hito.Observaciones,
                FechaAlta = hito.FechaAlta,
                UsuarioAlta = hito.UsuarioAlta
            };
        }

        private static SaldoComercialResponse MapSaldo(string externoId, IEnumerable<AcuerdoComercial> acuerdos)
        {
            var vias = acuerdos.SelectMany(a => a.Vias).Where(IsViaActiva).ToList();
            var totalPrometido = vias.Sum(GetMontoVigenteVia);
            var totalPagado = vias.Sum(GetTotalPagadoVia);
            return new SaldoComercialResponse
            {
                ExternoId = externoId,
                TotalPrometido = totalPrometido,
                TotalPagado = totalPagado,
                SaldoRestante = Math.Max(totalPrometido - totalPagado, 0)
            };
        }

        private readonly record struct SituacionFacturacionBalance(decimal FacturadoConfirmado, decimal ReservadoBorrador)
        {
            public static SituacionFacturacionBalance Empty => new(0, 0);
        }
    }
}

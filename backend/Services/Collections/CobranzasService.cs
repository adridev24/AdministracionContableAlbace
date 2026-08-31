using System.Data;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Collections
{
    public class CobranzasService : ICobranzasService
    {
        private const int MaxPageSize = 100;
        private const string CodigoOperacionCobranzaCliente = "COBRANZA_CLIENTE";
        private const string ModuloOrigenCobranzas = "COBRANZAS";
        private const string ConceptoClientes = "CLIENTES";

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;
        private readonly IExternalDataService _externalDataService;
        private readonly IContabilizacionAutomaticaService _contabilizacionAutomatica;
        private readonly IConfiguracionesContablesService _configuracionesContables;

        public CobranzasService(
            AppDbContext db,
            IUserContext userContext,
            IExternalDataService externalDataService,
            IContabilizacionAutomaticaService contabilizacionAutomatica,
            IConfiguracionesContablesService configuracionesContables)
        {
            _db = db;
            _userContext = userContext;
            _externalDataService = externalDataService;
            _contabilizacionAutomatica = contabilizacionAutomatica;
            _configuracionesContables = configuracionesContables;
        }

        public async Task<IEnumerable<MedioPagoCobranzaResponse>> GetMediosPagoDisponiblesAsync(bool soloActivos = false)
        {
            var query = _db.MediosPagoCobranza.AsNoTracking();
            if (soloActivos) query = query.Where(m => m.Activo);

            var medios = await query.OrderBy(m => m.Orden).ThenBy(m => m.Descripcion).ToListAsync();
            return medios.Select(MapMedioPagoDisponible);
        }

        public async Task<IEnumerable<BancoCobranzaResponse>> GetBancosDisponiblesAsync(bool soloActivos = false)
        {
            var query = _db.BancosCobranza.AsNoTracking();
            if (soloActivos) query = query.Where(b => b.Activo);

            var bancos = await query.OrderBy(b => b.Orden).ThenBy(b => b.Nombre).ToListAsync();
            return bancos.Select(MapBancoDisponible);
        }

        public async Task<CobranzaListResponse> GetCobranzasAsync(CobranzaListFilterRequest filter)
        {
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
            var query = GetCobranzaQuery(false);

            if (!string.IsNullOrWhiteSpace(filter.ClienteExternoId))
            {
                var clienteId = filter.ClienteExternoId.Trim();
                query = query.Where(c => c.ClienteExternoId == clienteId);
            }

            if (filter.FechaDesde.HasValue)
            {
                var desde = NormalizeDateOnlyUtc(filter.FechaDesde.Value);
                query = query.Where(c => c.Fecha >= desde);
            }

            if (filter.FechaHasta.HasValue)
            {
                var hasta = NormalizeDateOnlyUtc(filter.FechaHasta.Value).AddDays(1);
                query = query.Where(c => c.Fecha < hasta);
            }

            if (!string.IsNullOrWhiteSpace(filter.MonedaCodigo))
            {
                var moneda = NormalizeCurrency(filter.MonedaCodigo);
                query = query.Where(c => c.MonedaCodigo == moneda);
            }

            if (filter.Estado.HasValue)
            {
                query = query.Where(c => c.Estado == filter.Estado.Value);
            }

            var total = await query.CountAsync();
            var cobranzas = await query
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var clientes = await GetClientesAsync(cobranzas.Select(c => c.ClienteExternoId));
            var obras = await GetObrasAsync(cobranzas.SelectMany(c => c.AplicacionesFactura.Select(a => a.Venta.ObraExternaId)));

            return new CobranzaListResponse
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = cobranzas.Select(c => MapCobranza(c, clientes, obras)).ToList()
            };
        }

        public async Task<CobranzaResponse?> GetCobranzaAsync(int id)
        {
            var cobranza = await GetCobranzaQuery(false).FirstOrDefaultAsync(c => c.Id == id);
            if (cobranza == null) return null;

            var clientes = await GetClientesAsync(new[] { cobranza.ClienteExternoId });
            var obras = await GetObrasAsync(cobranza.AplicacionesFactura.Select(a => a.Venta.ObraExternaId));
            return MapCobranza(cobranza, clientes, obras);
        }

        public async Task<CobranzaResponse> CreateCobranzaAsync(CobranzaHeaderRequest request)
        {
            await ValidateClienteAsync(request.ClienteExternoId);

            var cobranza = new Cobranza
            {
                ClienteExternoId = NormalizeRequired(request.ClienteExternoId, "El cliente es obligatorio."),
                Fecha = NormalizeDateOnlyUtc(request.Fecha),
                MonedaCodigo = NormalizeCurrency(request.MonedaCodigo),
                Cotizacion = RoundExchange(request.Cotizacion),
                ImporteTotal = RoundMoney(request.ImporteTotal),
                Estado = CobranzaEstado.Borrador,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            };

            ValidateHeader(cobranza);
            _db.Cobranzas.Add(cobranza);
            await _db.SaveChangesAsync();
            return (await GetCobranzaAsync(cobranza.Id))!;
        }

        public async Task<CobranzaResponse> UpdateCobranzaAsync(int id, CobranzaHeaderRequest request)
        {
            var cobranza = await _db.Cobranzas.FirstOrDefaultAsync(c => c.Id == id);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");
            EnsureBorrador(cobranza);
            await ValidateClienteAsync(request.ClienteExternoId);

            cobranza.ClienteExternoId = NormalizeRequired(request.ClienteExternoId, "El cliente es obligatorio.");
            cobranza.Fecha = NormalizeDateOnlyUtc(request.Fecha);
            cobranza.MonedaCodigo = NormalizeCurrency(request.MonedaCodigo);
            cobranza.Cotizacion = RoundExchange(request.Cotizacion);
            cobranza.ImporteTotal = RoundMoney(request.ImporteTotal);
            cobranza.Observaciones = NormalizeOptional(request.Observaciones);
            cobranza.FechaModificacion = DateTime.UtcNow;
            cobranza.UsuarioModificacion = _userContext.UserName;
            ValidateHeader(cobranza);

            await _db.SaveChangesAsync();
            return (await GetCobranzaAsync(id))!;
        }

        public async Task<IEnumerable<FacturaPendienteCobranzaResponse>> GetFacturasDisponiblesAsync(int cobranzaId)
        {
            var cobranza = await _db.Cobranzas
                .AsNoTracking()
                .Include(c => c.MediosPago)
                .Include(c => c.AplicacionesFactura)
                .FirstOrDefaultAsync(c => c.Id == cobranzaId);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");

            var ventas = await _db.Ventas
                .AsNoTracking()
                .Include(v => v.TipoComprobante)
                .Where(v => v.Estado == VentaEstado.Confirmada &&
                    v.ClienteExternoId == cobranza.ClienteExternoId &&
                    v.MonedaCodigo == cobranza.MonedaCodigo)
                .OrderBy(v => v.FechaComprobante)
                .ThenBy(v => v.Id)
                .ToListAsync();

            var balances = await BuildFacturaBalancesAsync(ventas, cobranza.Id);
            var obras = new Dictionary<string, Obra>();
            return ventas
                .Select(v => MapFacturaDisponible(v, balances[v.Id], obras))
                .Where(f => f.SaldoDisponible > 0)
                .ToList();
        }

        public async Task<IEnumerable<CobranzaAplicacionFacturaResponse>> GetAplicacionesFacturaAsync(int cobranzaId)
        {
            var cobranza = await GetCobranzaQuery(false).FirstOrDefaultAsync(c => c.Id == cobranzaId);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");

            var obras = await GetObrasAsync(cobranza.AplicacionesFactura.Select(a => a.Venta.ObraExternaId));
            var responses = new List<CobranzaAplicacionFacturaResponse>();
            foreach (var aplicacion in cobranza.AplicacionesFactura.OrderBy(a => a.Venta.FechaComprobante).ThenBy(a => a.Id))
            {
                var balances = await BuildFacturaBalancesAsync(new[] { aplicacion.Venta }, cobranza.Id, aplicacion.Id);
                responses.Add(MapAplicacionFactura(aplicacion, obras, balances[aplicacion.VentaId]));
            }

            return responses;
        }

        public async Task<CobranzaResponse> AddMedioPagoAsync(int cobranzaId, CobranzaMedioPagoRequest request)
        {
            var cobranza = await _db.Cobranzas.FirstOrDefaultAsync(c => c.Id == cobranzaId);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");
            EnsureBorrador(cobranza);
            var validated = await ValidateMedioPagoAsync(request);

            _db.CobranzasMediosPago.Add(new CobranzaMedioPago
            {
                CobranzaId = cobranzaId,
                MedioPagoCobranzaId = validated.Medio.Id,
                BancoCobranzaId = validated.Banco?.Id,
                Importe = RoundMoney(request.Importe),
                Banco = NormalizeOptional(validated.Banco?.Nombre ?? request.Banco),
                NumeroReferencia = NormalizeOptional(request.NumeroReferencia),
                FechaValor = request.FechaValor.HasValue ? NormalizeDateOnlyUtc(request.FechaValor.Value) : null,
                Observaciones = NormalizeOptional(request.Observaciones),
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = _userContext.UserName
            });

            await _db.SaveChangesAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaResponse> UpdateMedioPagoAsync(int cobranzaId, int medioId, CobranzaMedioPagoRequest request)
        {
            var medioCobranza = await _db.CobranzasMediosPago
                .Include(m => m.Cobranza)
                .FirstOrDefaultAsync(m => m.Id == medioId && m.CobranzaId == cobranzaId);
            if (medioCobranza == null) throw new InvalidOperationException("Medio de pago de cobranza no encontrado.");
            EnsureBorrador(medioCobranza.Cobranza);
            var validated = await ValidateMedioPagoAsync(request);

            medioCobranza.MedioPagoCobranzaId = validated.Medio.Id;
            medioCobranza.BancoCobranzaId = validated.Banco?.Id;
            medioCobranza.Importe = RoundMoney(request.Importe);
            medioCobranza.Banco = NormalizeOptional(validated.Banco?.Nombre ?? request.Banco);
            medioCobranza.NumeroReferencia = NormalizeOptional(request.NumeroReferencia);
            medioCobranza.FechaValor = request.FechaValor.HasValue ? NormalizeDateOnlyUtc(request.FechaValor.Value) : null;
            medioCobranza.Observaciones = NormalizeOptional(request.Observaciones);
            medioCobranza.FechaModificacion = DateTime.UtcNow;
            medioCobranza.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaResponse> DeleteMedioPagoAsync(int cobranzaId, int medioId)
        {
            var medio = await _db.CobranzasMediosPago
                .Include(m => m.Cobranza)
                .FirstOrDefaultAsync(m => m.Id == medioId && m.CobranzaId == cobranzaId);
            if (medio == null) throw new InvalidOperationException("Medio de pago de cobranza no encontrado.");
            EnsureBorrador(medio.Cobranza);

            _db.CobranzasMediosPago.Remove(medio);
            await _db.SaveChangesAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaResponse> AddAplicacionFacturaAsync(int cobranzaId, CobranzaAplicacionFacturaRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cobranza = await GetCobranzaForMutationAsync(cobranzaId);
            EnsureBorrador(cobranza);
            var existing = cobranza.AplicacionesFactura.FirstOrDefault(a => a.VentaId == request.VentaId);
            await ValidateAplicacionFacturaAsync(cobranza, request, existing?.Id);

            if (existing != null)
            {
                existing.ImporteAplicado = RoundMoney(request.ImporteAplicado);
                existing.FechaModificacion = DateTime.UtcNow;
                existing.UsuarioModificacion = _userContext.UserName;
            }
            else
            {
                cobranza.AplicacionesFactura.Add(new CobranzaAplicacionFactura
                {
                    CobranzaId = cobranzaId,
                    VentaId = request.VentaId,
                    ImporteAplicado = RoundMoney(request.ImporteAplicado),
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaResponse> UpdateAplicacionFacturaAsync(int cobranzaId, int aplicacionId, CobranzaAplicacionFacturaRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cobranza = await GetCobranzaForMutationAsync(cobranzaId);
            EnsureBorrador(cobranza);
            var aplicacion = cobranza.AplicacionesFactura.FirstOrDefault(a => a.Id == aplicacionId);
            if (aplicacion == null) throw new InvalidOperationException("Aplicacion de factura no encontrada.");
            await ValidateAplicacionFacturaAsync(cobranza, request, aplicacionId);

            aplicacion.VentaId = request.VentaId;
            aplicacion.ImporteAplicado = RoundMoney(request.ImporteAplicado);
            aplicacion.FechaModificacion = DateTime.UtcNow;
            aplicacion.UsuarioModificacion = _userContext.UserName;
            _db.CobranzasAplicacionesObligacion.RemoveRange(aplicacion.AplicacionesObligacion);
            aplicacion.AplicacionesObligacion.Clear();

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaResponse> DeleteAplicacionFacturaAsync(int cobranzaId, int aplicacionId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var aplicacion = await _db.CobranzasAplicacionesFactura
                .Include(a => a.Cobranza)
                .Include(a => a.AplicacionesObligacion)
                .FirstOrDefaultAsync(a => a.Id == aplicacionId && a.CobranzaId == cobranzaId);
            if (aplicacion == null) throw new InvalidOperationException("Aplicacion de factura no encontrada.");
            EnsureBorrador(aplicacion.Cobranza);

            _db.CobranzasAplicacionesObligacion.RemoveRange(aplicacion.AplicacionesObligacion);
            _db.CobranzasAplicacionesFactura.Remove(aplicacion);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (await GetCobranzaAsync(cobranzaId))!;
        }

        public async Task<CobranzaConfirmacionResponse> ConfirmarCobranzaAsync(int cobranzaId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cobranza = await GetCobranzaForMutationAsync(cobranzaId);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");
            EnsureBorrador(cobranza);

            ValidateConfirmacionBasica(cobranza);
            await ValidateConfiguracionContableAsync(cobranza);

            foreach (var aplicacion in cobranza.AplicacionesFactura)
            {
                _db.CobranzasAplicacionesObligacion.RemoveRange(aplicacion.AplicacionesObligacion);
                aplicacion.AplicacionesObligacion.Clear();
                await ValidateAplicacionFacturaAsync(cobranza, new CobranzaAplicacionFacturaRequest
                {
                    VentaId = aplicacion.VentaId,
                    ImporteAplicado = aplicacion.ImporteAplicado
                }, aplicacion.Id);
                await RebuildDistribucionAsync(aplicacion, cobranza);
            }

            await _db.SaveChangesAsync();
            await AplicarDistribucionACuotasAsync(cobranza);
            var asiento = await _contabilizacionAutomatica.GenerarAsientoAutomaticoAsync(await BuildSolicitudContableAsync(cobranza));
            var movimientoIds = await EnsureMovimientosCuentaCorrienteAsync(cobranza);

            var now = DateTime.UtcNow;
            cobranza.Estado = CobranzaEstado.Confirmada;
            cobranza.FechaConfirmacion = now;
            cobranza.UsuarioConfirmacion = _userContext.UserName;
            cobranza.AsientoContableId = asiento.AsientoContableId;
            cobranza.FechaModificacion = now;
            cobranza.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CobranzaConfirmacionResponse
            {
                Cobranza = (await GetCobranzaAsync(cobranzaId))!,
                AsientoContableId = asiento.AsientoContableId,
                AsientoYaExistia = asiento.YaExistia,
                CodigoOperacionContable = CodigoOperacionCobranzaCliente,
                ConceptosContables = BuildConceptosContables(cobranza),
                TotalAplicadoFacturas = cobranza.AplicacionesFactura.Sum(a => a.ImporteAplicado),
                TotalMediosCancelacion = cobranza.MediosPago.Sum(m => m.Importe),
                MovimientoCuentaCorrienteId = movimientoIds.FirstOrDefault()
            };
        }

        private async Task<Cobranza> GetCobranzaForMutationAsync(int cobranzaId)
        {
            var cobranza = await GetCobranzaQuery(true).FirstOrDefaultAsync(c => c.Id == cobranzaId);
            if (cobranza == null) throw new InvalidOperationException("Cobranza no encontrada.");
            return cobranza;
        }

        private IQueryable<Cobranza> GetCobranzaQuery(bool tracking)
        {
            var query = _db.Cobranzas
                .Include(c => c.MediosPago)
                    .ThenInclude(m => m.MedioPago)
                .Include(c => c.MediosPago)
                    .ThenInclude(m => m.BancoCatalogo)
                .Include(c => c.AplicacionesFactura)
                    .ThenInclude(a => a.Venta)
                        .ThenInclude(v => v.TipoComprobante)
                .Include(c => c.AplicacionesFactura)
                    .ThenInclude(a => a.AplicacionesObligacion)
                        .ThenInclude(o => o.CuotaComercial)
                .AsQueryable();

            return tracking ? query : query.AsNoTracking();
        }

        private async Task ValidateAplicacionFacturaAsync(Cobranza cobranza, CobranzaAplicacionFacturaRequest request, int? aplicacionId)
        {
            var venta = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == request.VentaId);
            if (venta == null) throw new InvalidOperationException("Factura no encontrada.");
            if (venta.Estado != VentaEstado.Confirmada) throw new InvalidOperationException("Solo se pueden aplicar facturas confirmadas.");
            if (venta.ClienteExternoId != cobranza.ClienteExternoId) throw new InvalidOperationException("La factura no pertenece al cliente de la cobranza.");
            if (venta.MonedaCodigo != cobranza.MonedaCodigo)
            {
                throw new InvalidOperationException($"La cobranza esta cargada en {cobranza.MonedaCodigo} y la factura esta en {venta.MonedaCodigo}. Solo se pueden aplicar pagos a facturas de la misma moneda.");
            }
            if (RoundMoney(request.ImporteAplicado) <= 0) throw new InvalidOperationException("El importe aplicado debe ser mayor a cero.");

            if (cobranza.AplicacionesFactura.Any(a => a.Id != aplicacionId && a.VentaId == request.VentaId))
            {
                throw new InvalidOperationException("La factura ya se encuentra aplicada en esta cobranza.");
            }

            var balances = await BuildFacturaBalancesAsync(new[] { venta }, cobranza.Id, aplicacionId);
            var saldoDisponible = balances[venta.Id].SaldoDisponible;
            if (RoundMoney(request.ImporteAplicado) > saldoDisponible)
            {
                throw new InvalidOperationException("El importe aplicado supera el saldo disponible de la factura.");
            }

            var totalAplicadoCobranza = cobranza.AplicacionesFactura
                .Where(a => a.Id != aplicacionId)
                .Sum(a => a.ImporteAplicado) + RoundMoney(request.ImporteAplicado);
            if (totalAplicadoCobranza > cobranza.ImporteTotal)
            {
                throw new InvalidOperationException("Las aplicaciones superan el importe total de la cobranza.");
            }
        }

        private async Task RebuildDistribucionAsync(CobranzaAplicacionFactura aplicacion, Cobranza cobranza)
        {
            var facturaId = aplicacion.VentaId.ToString();
            var vinculaciones = await _db.VinculacionesFacturaComerciales
                .Include(v => v.CuotaComercial)
                    .ThenInclude(c => c.PlanPago)
                        .ThenInclude(p => p.AcuerdoComercialVia)
                            .ThenInclude(v => v.AcuerdoComercial)
                .Where(v => v.FacturaExternaId == facturaId)
                .ToListAsync();

            var via1 = vinculaciones
                .Where(v => v.CuotaComercial.PlanPago.AcuerdoComercialVia.ViaOperacion == ViaOperacion.Via1)
                .Where(v => v.CuotaComercial.PlanPago.AcuerdoComercialVia.MonedaCodigo == cobranza.MonedaCodigo)
                .Where(v => v.CuotaComercial.PlanPago.AcuerdoComercialVia.AcuerdoComercial.ClienteExternoId == cobranza.ClienteExternoId)
                .ToList();

            if (!via1.Any())
            {
                return;
            }

            var grupos = via1
                .GroupBy(v => v.CuotaComercialId)
                .Select(g => new
                {
                    Cuota = g.First().CuotaComercial,
                    ImporteFacturado = RoundMoney(g.Sum(v => v.ImporteVinculado))
                })
                .OrderBy(g => g.Cuota.TipoCuota == TipoCuota.Anticipo ? 0 : 1)
                .ThenBy(g => g.Cuota.FechaVencimiento)
                .ThenBy(g => g.Cuota.NumeroCuota)
                .ToList();

            var cuotaIds = grupos.Select(g => g.Cuota.Id).ToList();
            var confirmadoPorCuota = await _db.CobranzasAplicacionesObligacion
                .AsNoTracking()
                .Where(o => cuotaIds.Contains(o.CuotaComercialId) &&
                    o.AplicacionFactura.VentaId == aplicacion.VentaId &&
                    o.AplicacionFactura.Cobranza.Estado == CobranzaEstado.Confirmada)
                .GroupBy(o => o.CuotaComercialId)
                .Select(g => new { CuotaId = g.Key, Importe = g.Sum(x => x.ImporteAplicado) })
                .ToDictionaryAsync(x => x.CuotaId, x => x.Importe);

            var restante = RoundMoney(aplicacion.ImporteAplicado);
            foreach (var grupo in grupos)
            {
                if (restante <= 0) break;
                confirmadoPorCuota.TryGetValue(grupo.Cuota.Id, out var yaCobrado);
                var pendienteFacturado = RoundMoney(grupo.ImporteFacturado - yaCobrado);
                if (pendienteFacturado <= 0) continue;

                var importe = Math.Min(restante, pendienteFacturado);
                aplicacion.AplicacionesObligacion.Add(new CobranzaAplicacionObligacion
                {
                    CuotaComercialId = grupo.Cuota.Id,
                    TipoObligacion = grupo.Cuota.TipoCuota.ToString(),
                    ImporteAplicado = RoundMoney(importe),
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                });
                restante = RoundMoney(restante - importe);
            }

            if (restante > 0)
            {
                throw new InvalidOperationException("La factura no posee saldo facturado pendiente suficiente en obligaciones de Via 1.");
            }
        }

        private async Task AplicarDistribucionACuotasAsync(Cobranza cobranza)
        {
            var distribuciones = cobranza.AplicacionesFactura.SelectMany(a => a.AplicacionesObligacion).ToList();
            var cuotaIds = distribuciones.Select(d => d.CuotaComercialId).Distinct().ToList();
            var cuotas = await _db.CuotasComerciales.Where(c => cuotaIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id);

            foreach (var item in distribuciones)
            {
                if (!cuotas.TryGetValue(item.CuotaComercialId, out var cuota))
                {
                    throw new InvalidOperationException("No se encontro una obligacion de Via 1 para aplicar la cobranza.");
                }

                if (item.ImporteAplicado > cuota.SaldoPendiente)
                {
                    throw new InvalidOperationException($"La cobranza supera el saldo pendiente de la obligacion {cuota.Id}.");
                }

                cuota.ImportePagado = RoundMoney(cuota.ImportePagado + item.ImporteAplicado);
                cuota.SaldoPendiente = Math.Max(RoundMoney(cuota.ImporteOriginal - cuota.ImportePagado), 0);
                UpdateCuotaEstado(cuota);
            }
        }

        private async Task<List<int>> EnsureMovimientosCuentaCorrienteAsync(Cobranza cobranza)
        {
            var ids = new List<int>();
            foreach (var aplicacion in cobranza.AplicacionesFactura)
            {
                var tipoMovimiento = BuildTipoMovimientoCobranza(aplicacion.VentaId);
                var idOrigen = cobranza.Id.ToString();
                var existing = await _db.VentasMovimientosCuentaCorriente.FirstOrDefaultAsync(m =>
                    m.ModuloOrigen == ModuloOrigenCobranzas &&
                    m.IdOrigen == idOrigen &&
                    m.TipoMovimiento == tipoMovimiento);
                if (existing != null)
                {
                    ids.Add(existing.Id);
                    continue;
                }

                var movimiento = new VentaMovimientoCuentaCorriente
                {
                    ClienteExternoId = cobranza.ClienteExternoId,
                    ObraExternaId = aplicacion.Venta.ObraExternaId,
                    Fecha = cobranza.Fecha,
                    TipoMovimiento = tipoMovimiento,
                    Debe = 0,
                    Haber = aplicacion.ImporteAplicado,
                    ModuloOrigen = ModuloOrigenCobranzas,
                    IdOrigen = idOrigen,
                    Descripcion = $"Cobranza {cobranza.Id} aplicada a factura {BuildComprobante(aplicacion.Venta)}",
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                };
                _db.VentasMovimientosCuentaCorriente.Add(movimiento);
                await _db.SaveChangesAsync();
                ids.Add(movimiento.Id);
            }

            return ids;
        }

        private async Task<SolicitudContabilizacionAutomaticaRequest> BuildSolicitudContableAsync(Cobranza cobranza)
        {
            var importes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                [ConceptoClientes] = RoundMoney(cobranza.AplicacionesFactura.Sum(a => a.ImporteAplicado))
            };

            foreach (var grupo in cobranza.MediosPago.GroupBy(m => m.MedioPago.CodigoConceptoContable))
            {
                importes[grupo.Key] = RoundMoney(grupo.Sum(m => m.Importe));
            }

            return new SolicitudContabilizacionAutomaticaRequest
            {
                CodigoOperacion = CodigoOperacionCobranzaCliente,
                ModuloOrigen = ModuloOrigenCobranzas,
                IdOrigen = cobranza.Id.ToString(),
                Fecha = cobranza.Fecha,
                Descripcion = $"Cobranza {await GetClienteDescripcionAsync(cobranza.ClienteExternoId)}",
                ImportesPorConcepto = importes
            };
        }

        private async Task<string> GetClienteDescripcionAsync(string clienteExternoId)
        {
            try
            {
                if (int.TryParse(clienteExternoId, out var clienteId))
                {
                    var cliente = await _externalDataService.GetClientByIdAsync(clienteId);
                    if (!string.IsNullOrWhiteSpace(cliente?.NombreCliente))
                    {
                        return cliente.NombreCliente.Trim();
                    }
                }
            }
            catch
            {
            }

            return clienteExternoId;
        }

        private async Task ValidateConfiguracionContableAsync(Cobranza cobranza)
        {
            var configuracion = await _configuracionesContables.GetConfiguracionPorOperacionAsync(CodigoOperacionCobranzaCliente);
            if (configuracion == null || !configuracion.Activa)
            {
                throw new InvalidOperationException("La configuracion contable COBRANZA_CLIENTE no existe o esta inactiva.");
            }

            var conceptos = configuracion.Detalles.Select(d => d.Concepto).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var concepto in BuildConceptosContables(cobranza))
            {
                if (!conceptos.Contains(concepto))
                {
                    throw new InvalidOperationException($"La configuracion contable COBRANZA_CLIENTE no posee el concepto {concepto}.");
                }
            }
        }

        private static List<string> BuildConceptosContables(Cobranza cobranza)
        {
            return new[] { ConceptoClientes }
                .Concat(cobranza.MediosPago.Select(m => m.MedioPago.CodigoConceptoContable))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void ValidateConfirmacionBasica(Cobranza cobranza)
        {
            var totalMedios = RoundMoney(cobranza.MediosPago.Sum(m => m.Importe));
            var totalAplicado = RoundMoney(cobranza.AplicacionesFactura.Sum(a => a.ImporteAplicado));
            if (!cobranza.MediosPago.Any()) throw new InvalidOperationException("Debe informar al menos un medio de pago.");
            if (!cobranza.AplicacionesFactura.Any()) throw new InvalidOperationException("Debe aplicar la cobranza a al menos una factura.");
            if (totalMedios != cobranza.ImporteTotal) throw new InvalidOperationException("La suma de medios de pago debe coincidir con el importe total.");
            if (totalAplicado != cobranza.ImporteTotal) throw new InvalidOperationException("La suma aplicada a facturas debe coincidir con el importe total.");
        }

        private async Task<ValidatedMedioPago> ValidateMedioPagoAsync(CobranzaMedioPagoRequest request)
        {
            var medio = await _db.MediosPagoCobranza.FirstOrDefaultAsync(m => m.Id == request.MedioPagoCobranzaId);
            if (medio == null) throw new InvalidOperationException("Medio de pago no encontrado.");
            if (!medio.Activo) throw new InvalidOperationException("El medio de pago se encuentra inactivo.");
            if (RoundMoney(request.Importe) <= 0) throw new InvalidOperationException("El importe del medio de pago debe ser mayor a cero.");
            if (medio.RequiereReferencia && string.IsNullOrWhiteSpace(request.NumeroReferencia)) throw new InvalidOperationException("El medio de pago requiere referencia.");
            if (medio.RequiereFechaValor && !request.FechaValor.HasValue) throw new InvalidOperationException("El medio de pago requiere fecha valor.");

            BancoCobranza? banco = null;
            if (medio.RequiereBanco)
            {
                if (!request.BancoCobranzaId.HasValue) throw new InvalidOperationException("El medio de pago requiere seleccionar un banco.");
                banco = await _db.BancosCobranza.FirstOrDefaultAsync(b => b.Id == request.BancoCobranzaId.Value);
                if (banco == null) throw new InvalidOperationException("Banco no encontrado.");
                if (!banco.Activo) throw new InvalidOperationException("El banco seleccionado se encuentra inactivo.");
            }

            return new ValidatedMedioPago(medio, banco);
        }

        private async Task ValidateClienteAsync(string clienteExternoId)
        {
            var value = NormalizeRequired(clienteExternoId, "El cliente es obligatorio.");
            if (!int.TryParse(value, out var clienteId))
            {
                throw new InvalidOperationException("El identificador de cliente no es valido.");
            }

            var cliente = await _externalDataService.GetClientByIdAsync(clienteId);
            if (cliente == null) throw new InvalidOperationException("Cliente inexistente.");
        }

        private static void ValidateHeader(Cobranza cobranza)
        {
            if (cobranza.ImporteTotal <= 0) throw new InvalidOperationException("El importe total debe ser mayor a cero.");
            if (cobranza.Cotizacion <= 0) throw new InvalidOperationException("La cotizacion debe ser mayor a cero.");
        }

        private async Task<Dictionary<int, FacturaBalance>> BuildFacturaBalancesAsync(IEnumerable<Venta> ventas, int cobranzaId, int? aplicacionId = null)
        {
            var ventaIds = ventas.Select(v => v.Id).Distinct().ToList();
            var confirmadas = await _db.CobranzasAplicacionesFactura
                .AsNoTracking()
                .Where(a => ventaIds.Contains(a.VentaId) && a.Cobranza.Estado == CobranzaEstado.Confirmada)
                .GroupBy(a => a.VentaId)
                .Select(g => new { VentaId = g.Key, Importe = g.Sum(a => a.ImporteAplicado) })
                .ToDictionaryAsync(x => x.VentaId, x => x.Importe);

            var reservas = await _db.CobranzasAplicacionesFactura
                .AsNoTracking()
                .Where(a => ventaIds.Contains(a.VentaId) &&
                    a.Cobranza.Estado == CobranzaEstado.Borrador &&
                    a.CobranzaId != cobranzaId &&
                    (!aplicacionId.HasValue || a.Id != aplicacionId.Value))
                .GroupBy(a => a.VentaId)
                .Select(g => new { VentaId = g.Key, Importe = g.Sum(a => a.ImporteAplicado) })
                .ToDictionaryAsync(x => x.VentaId, x => x.Importe);

            return ventas.ToDictionary(v => v.Id, v =>
            {
                confirmadas.TryGetValue(v.Id, out var cobrado);
                reservas.TryGetValue(v.Id, out var reservado);
                return new FacturaBalance(RoundMoney(cobrado), RoundMoney(reservado), Math.Max(RoundMoney(v.Total - cobrado - reservado), 0));
            });
        }

        private async Task<Dictionary<string, Client>> GetClientesAsync(IEnumerable<string> clienteIds)
        {
            var ids = clienteIds.Distinct().ToHashSet();
            if (ids.Count == 0) return new Dictionary<string, Client>();
            var clientes = await _externalDataService.GetClientsAsync();
            return clientes
                .Where(c => ids.Contains(c.IdCliente.ToString()))
                .ToDictionary(c => c.IdCliente.ToString());
        }

        private async Task<Dictionary<string, Obra>> GetObrasAsync(IEnumerable<string> obraIds)
        {
            var ids = obraIds.Distinct().ToHashSet();
            if (ids.Count == 0) return new Dictionary<string, Obra>();
            try
            {
                var obras = await _externalDataService.GetObrasAsync();
                return obras
                    .Where(o => ids.Contains(o.IdObra.ToString()))
                    .ToDictionary(o => o.IdObra.ToString());
            }
            catch
            {
                return new Dictionary<string, Obra>();
            }
        }

        private static CobranzaResponse MapCobranza(Cobranza cobranza, IReadOnlyDictionary<string, Client> clientes, IReadOnlyDictionary<string, Obra> obras)
        {
            clientes.TryGetValue(cobranza.ClienteExternoId, out var cliente);
            return new CobranzaResponse
            {
                Id = cobranza.Id,
                ClienteExternoId = cobranza.ClienteExternoId,
                ClienteNombre = cliente?.NombreCliente,
                Fecha = cobranza.Fecha,
                MonedaCodigo = cobranza.MonedaCodigo,
                Cotizacion = cobranza.Cotizacion,
                ImporteTotal = cobranza.ImporteTotal,
                Estado = cobranza.Estado,
                Observaciones = cobranza.Observaciones,
                FechaAlta = cobranza.FechaAlta,
                UsuarioAlta = cobranza.UsuarioAlta,
                FechaModificacion = cobranza.FechaModificacion,
                UsuarioModificacion = cobranza.UsuarioModificacion,
                FechaConfirmacion = cobranza.FechaConfirmacion,
                UsuarioConfirmacion = cobranza.UsuarioConfirmacion,
                AsientoContableId = cobranza.AsientoContableId,
                TotalMedios = cobranza.MediosPago.Sum(m => m.Importe),
                TotalAplicado = cobranza.AplicacionesFactura.Sum(a => a.ImporteAplicado),
                CantidadFacturasAplicadas = cobranza.AplicacionesFactura.Count,
                MediosPago = cobranza.MediosPago.OrderBy(m => m.Id).Select(MapMedioPago).ToList(),
                AplicacionesFactura = cobranza.AplicacionesFactura.OrderBy(a => a.Venta.FechaComprobante).Select(a => MapAplicacionFactura(a, obras, null)).ToList()
            };
        }

        private static MedioPagoCobranzaResponse MapMedioPagoDisponible(MedioPagoCobranza medio)
        {
            return new MedioPagoCobranzaResponse
            {
                Id = medio.Id,
                Codigo = medio.Codigo,
                Descripcion = medio.Descripcion,
                CodigoConceptoContable = medio.CodigoConceptoContable,
                Activo = medio.Activo,
                RequiereReferencia = medio.RequiereReferencia,
                RequiereBanco = medio.RequiereBanco,
                RequiereFechaValor = medio.RequiereFechaValor,
                Orden = medio.Orden
            };
        }

        private static BancoCobranzaResponse MapBancoDisponible(BancoCobranza banco)
        {
            return new BancoCobranzaResponse
            {
                Id = banco.Id,
                Codigo = banco.Codigo,
                Nombre = banco.Nombre,
                Activo = banco.Activo,
                Orden = banco.Orden
            };
        }

        private static CobranzaMedioPagoResponse MapMedioPago(CobranzaMedioPago medio)
        {
            return new CobranzaMedioPagoResponse
            {
                Id = medio.Id,
                CobranzaId = medio.CobranzaId,
                MedioPagoCobranzaId = medio.MedioPagoCobranzaId,
                MedioPagoCodigo = medio.MedioPago.Codigo,
                MedioPagoDescripcion = medio.MedioPago.Descripcion,
                CodigoConceptoContable = medio.MedioPago.CodigoConceptoContable,
                Importe = medio.Importe,
                BancoCobranzaId = medio.BancoCobranzaId,
                Banco = medio.BancoCatalogo?.Nombre ?? medio.Banco,
                NumeroReferencia = medio.NumeroReferencia,
                FechaValor = medio.FechaValor,
                Observaciones = medio.Observaciones
            };
        }

        private static CobranzaAplicacionFacturaResponse MapAplicacionFactura(CobranzaAplicacionFactura aplicacion, IReadOnlyDictionary<string, Obra> obras, FacturaBalance? balance)
        {
            obras.TryGetValue(aplicacion.Venta.ObraExternaId, out var obra);
            return new CobranzaAplicacionFacturaResponse
            {
                Id = aplicacion.Id,
                CobranzaId = aplicacion.CobranzaId,
                VentaId = aplicacion.VentaId,
                Comprobante = BuildComprobante(aplicacion.Venta),
                FechaComprobante = aplicacion.Venta.FechaComprobante,
                ObraExternaId = aplicacion.Venta.ObraExternaId,
                ObraNombre = obra?.NombreObra,
                TotalFactura = aplicacion.Venta.Total,
                ImporteAplicado = aplicacion.ImporteAplicado,
                CobradoConfirmadoSinEsta = balance?.CobradoConfirmado ?? 0,
                ReservadoBorradorSinEsta = balance?.ReservadoBorrador ?? 0,
                SaldoDisponibleSinEsta = balance?.SaldoDisponible ?? 0,
                AplicacionesObligacion = aplicacion.AplicacionesObligacion.OrderBy(o => o.CuotaComercial.FechaVencimiento).Select(MapAplicacionObligacion).ToList()
            };
        }

        private static CobranzaAplicacionObligacionResponse MapAplicacionObligacion(CobranzaAplicacionObligacion obligacion)
        {
            return new CobranzaAplicacionObligacionResponse
            {
                Id = obligacion.Id,
                CobranzaAplicacionFacturaId = obligacion.CobranzaAplicacionFacturaId,
                CuotaComercialId = obligacion.CuotaComercialId,
                TipoObligacion = obligacion.TipoObligacion,
                NumeroCuota = obligacion.CuotaComercial.NumeroCuota,
                FechaVencimiento = obligacion.CuotaComercial.FechaVencimiento,
                ImporteAplicado = obligacion.ImporteAplicado
            };
        }

        private static FacturaPendienteCobranzaResponse MapFacturaDisponible(Venta venta, FacturaBalance balance, IReadOnlyDictionary<string, Obra> obras)
        {
            obras.TryGetValue(venta.ObraExternaId, out var obra);
            return new FacturaPendienteCobranzaResponse
            {
                VentaId = venta.Id,
                TipoComprobante = venta.TipoComprobante?.Descripcion ?? venta.TipoComprobante?.Codigo ?? string.Empty,
                PuntoVenta = venta.PuntoVenta,
                Numero = venta.NumeroComprobante,
                Comprobante = BuildComprobante(venta),
                FechaComprobante = venta.FechaComprobante,
                ClienteExternoId = venta.ClienteExternoId,
                ObraExternaId = venta.ObraExternaId,
                ObraNombre = obra?.NombreObra,
                MonedaCodigo = venta.MonedaCodigo,
                Total = venta.Total,
                CobradoConfirmado = balance.CobradoConfirmado,
                ReservadoBorrador = balance.ReservadoBorrador,
                SaldoDisponible = balance.SaldoDisponible
            };
        }

        private static void EnsureBorrador(Cobranza cobranza)
        {
            if (cobranza.Estado != CobranzaEstado.Borrador)
            {
                throw new InvalidOperationException("Solo puede modificarse una cobranza en Borrador.");
            }
        }

        private static void UpdateCuotaEstado(CuotaComercial cuota)
        {
            if (cuota.Estado == CuotaEstado.Anulada) return;
            if (cuota.SaldoPendiente <= 0)
            {
                cuota.Estado = CuotaEstado.Pagada;
                return;
            }

            cuota.Estado = cuota.ImportePagado > 0 ? CuotaEstado.Parcial : CuotaEstado.Pendiente;
        }

        private static string BuildComprobante(Venta venta)
        {
            return $"{venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}";
        }

        private static string BuildTipoMovimientoCobranza(int ventaId)
        {
            return $"COBRANZA:{ventaId}";
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal RoundExchange(decimal value)
        {
            return Math.Round(value, 6, MidpointRounding.AwayFromZero);
        }

        private static string NormalizeCurrency(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "ARS" : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeRequired(string? value, string errorMessage)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException(errorMessage);
            return normalized;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static DateTime NormalizeDateOnlyUtc(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed record FacturaBalance(decimal CobradoConfirmado, decimal ReservadoBorrador, decimal SaldoDisponible);
        private sealed record ValidatedMedioPago(MedioPagoCobranza Medio, BancoCobranza? Banco);
    }
}

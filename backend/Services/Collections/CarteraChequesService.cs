using System.Data;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Treasury;
using BudgetControl.Api.Services.Accounting;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Collections
{
    public class CarteraChequesService : ICarteraChequesService
    {
        private const string CodigoMedioCheque = "CHEQUE";
        private const string CodigoOperacionRechazoChequeCliente = "RECHAZO_CHEQUE_CLIENTE";
        private const string CodigoOperacionAcreditacionCheque = "ACREDITACION_CHEQUE";
        private const string ModuloOrigenCarteraCheques = "CARTERA_CHEQUES";
        private const string ConceptoClientes = "CLIENTES";
        private const string ConceptoBanco = "BANCO";
        private const string ConceptoChequesTerceros = "CHEQUES_TERCEROS";

        private readonly AppDbContext _db;
        private readonly IUserContext _userContext;
        private readonly IContabilizacionAutomaticaService _contabilizacionAutomatica;

        public CarteraChequesService(
            AppDbContext db,
            IUserContext userContext,
            IContabilizacionAutomaticaService contabilizacionAutomatica)
        {
            _db = db;
            _userContext = userContext;
            _contabilizacionAutomatica = contabilizacionAutomatica;
        }

        public async Task<IEnumerable<ChequeTerceroListResponse>> GetChequesAsync(CarteraChequesFilterRequest filter)
        {
            var query = GetChequeQuery(false);

            if (filter.Estado.HasValue)
            {
                query = query.Where(c => c.Estado == filter.Estado.Value);
            }

            if (filter.FechaVencimientoDesde.HasValue)
            {
                var desde = NormalizeDateOnlyUtc(filter.FechaVencimientoDesde.Value);
                query = query.Where(c => c.FechaVencimiento >= desde);
            }

            if (filter.FechaVencimientoHasta.HasValue)
            {
                var hasta = NormalizeDateOnlyUtc(filter.FechaVencimientoHasta.Value).AddDays(1);
                query = query.Where(c => c.FechaVencimiento < hasta);
            }

            if (!string.IsNullOrWhiteSpace(filter.Moneda))
            {
                var moneda = NormalizeCurrency(filter.Moneda);
                query = query.Where(c => c.MonedaCodigo == moneda);
            }

            if (filter.BancoId.HasValue)
            {
                query = query.Where(c => c.BancoCobranzaId == filter.BancoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ClienteId))
            {
                var clienteId = filter.ClienteId.Trim();
                query = query.Where(c => c.CobranzaMedioPago.Cobranza.ClienteExternoId == clienteId);
            }

            var cheques = await query
                .OrderBy(c => c.FechaVencimiento)
                .ThenBy(c => c.Id)
                .ToListAsync();

            return cheques.Select(MapList);
        }

        public async Task<ChequeTerceroDetalleResponse?> GetChequeAsync(int id)
        {
            var cheque = await GetChequeQuery(false).FirstOrDefaultAsync(c => c.Id == id);
            return cheque == null ? null : MapDetalle(cheque);
        }

        public async Task<ChequeTerceroDetalleResponse> DepositarAsync(int id, DepositarChequeTerceroRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cheque = await GetChequeQuery(true).FirstOrDefaultAsync(c => c.Id == id);
            if (cheque == null) throw new InvalidOperationException("Cheque no encontrado.");
            if (cheque.Estado != ChequeTerceroEstado.EN_CARTERA)
            {
                throw new InvalidOperationException("Solo se pueden depositar cheques en cartera.");
            }

            var cuentaBancaria = await GetCuentaBancariaDepositoAsync(request.CuentaBancariaEmpresaId, cheque.MonedaCodigo);

            cheque.FechaDeposito = NormalizeRequiredDate(request.FechaDeposito, "La fecha de deposito es obligatoria.");
            cheque.CuentaBancariaEmpresaId = cuentaBancaria.Id;
            cheque.UsuarioDeposito = _userContext.UserName;
            cheque.Estado = ChequeTerceroEstado.DEPOSITADO;
            cheque.FechaModificacion = DateTime.UtcNow;
            cheque.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (await GetChequeAsync(id))!;
        }

        public async Task<ChequeTerceroDetalleResponse> AcreditarAsync(int id, AcreditarChequeTerceroRequest request)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cheque = await GetChequeQuery(true).FirstOrDefaultAsync(c => c.Id == id);
            if (cheque == null) throw new InvalidOperationException("Cheque no encontrado.");
            if (cheque.Estado != ChequeTerceroEstado.DEPOSITADO)
            {
                throw new InvalidOperationException("Solo se pueden acreditar cheques depositados.");
            }

            if (cheque.AsientoContableAcreditacionId.HasValue)
            {
                throw new InvalidOperationException("El cheque posee un asiento de acreditacion sin estar acreditado. Revise la consistencia de la cartera.");
            }

            if (!cheque.CuentaBancariaEmpresaId.HasValue)
            {
                throw new InvalidOperationException("Debe asociarse una cuenta bancaria propia antes de acreditar.");
            }

            var cuentaBancaria = await GetCuentaBancariaDepositoAsync(cheque.CuentaBancariaEmpresaId.Value, cheque.MonedaCodigo);
            cheque.FechaAcreditacion = NormalizeRequiredDate(request.FechaAcreditacion, "La fecha de acreditacion es obligatoria.");

            var asiento = await _contabilizacionAutomatica.GenerarAsientoAutomaticoAsync(
                BuildSolicitudContableAcreditacion(cheque, cuentaBancaria, cheque.FechaAcreditacion.Value));

            if (asiento.YaExistia)
            {
                throw new InvalidOperationException("Ya existe un asiento de acreditacion para este cheque.");
            }

            cheque.AsientoContableAcreditacionId = asiento.AsientoContableId;
            cheque.UsuarioAcreditacion = _userContext.UserName;
            cheque.Estado = ChequeTerceroEstado.ACREDITADO;
            cheque.FechaModificacion = DateTime.UtcNow;
            cheque.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return MapDetalle(cheque);
        }

        public async Task<ChequeTerceroDetalleResponse> RechazarAsync(int id, RechazarChequeTerceroRequest request)
        {
            var fechaRechazo = NormalizeRequiredDate(request.FechaRechazo, "La fecha de rechazo es obligatoria.");
            var motivo = NormalizeRequired(request.Motivo, "El motivo de rechazo es obligatorio.");

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cheque = await GetChequeQuery(true).FirstOrDefaultAsync(c => c.Id == id);
            if (cheque == null) throw new InvalidOperationException("Cheque no encontrado.");

            if (cheque.Estado == ChequeTerceroEstado.RECHAZADO)
            {
                throw new InvalidOperationException("El cheque ya fue rechazado.");
            }

            if (cheque.Estado != ChequeTerceroEstado.DEPOSITADO)
            {
                throw new InvalidOperationException("Solo se pueden rechazar cheques depositados.");
            }

            if (cheque.AsientoContableRechazoId.HasValue)
            {
                throw new InvalidOperationException("El cheque posee un asiento de rechazo sin estar rechazado. Revise la consistencia de la cartera.");
            }

            var cobranza = await GetCobranzaOrigenParaRechazoAsync(cheque.CobranzaMedioPago.CobranzaId);
            if (cobranza.Estado != CobranzaEstado.Confirmada)
            {
                throw new InvalidOperationException("Solo se pueden rechazar cheques de cobranzas confirmadas.");
            }

            await EnsureDistribucionMediosAplicacionesFacturaAsync(cobranza);
            await _db.Entry(cheque.CobranzaMedioPago)
                .Collection(m => m.AplicacionesFactura)
                .Query()
                .Include(d => d.AplicacionFactura)
                    .ThenInclude(a => a.Venta)
                .LoadAsync();

            var distribucionesCheque = cheque.CobranzaMedioPago.AplicacionesFactura
                .OrderBy(d => d.CobranzaAplicacionFacturaId)
                .ThenBy(d => d.Id)
                .ToList();

            var totalDistribuido = RoundMoney(distribucionesCheque.Sum(d => d.ImporteAplicado));
            if (totalDistribuido != RoundMoney(cheque.Importe))
            {
                throw new InvalidOperationException("La distribucion del cheque no coincide con su importe.");
            }

            var now = DateTime.UtcNow;
            await EnsureMovimientosRechazoCuentaCorrienteAsync(cheque, distribucionesCheque, fechaRechazo, now);

            cheque.Estado = ChequeTerceroEstado.RECHAZADO;
            cheque.FechaRechazo = fechaRechazo;
            cheque.MotivoRechazo = motivo;
            cheque.UsuarioRechazo = _userContext.UserName;
            cheque.FechaModificacion = now;
            cheque.UsuarioModificacion = _userContext.UserName;

            await _db.SaveChangesAsync();
            await RecalcularCuotasAfectadasPorRechazosAsync(distribucionesCheque);

            var asiento = await _contabilizacionAutomatica.GenerarAsientoAutomaticoAsync(BuildSolicitudContableRechazo(cheque, fechaRechazo, motivo));
            if (cheque.AsientoContableRechazoId.HasValue && cheque.AsientoContableRechazoId.Value != asiento.AsientoContableId)
            {
                throw new InvalidOperationException("El asiento de rechazo existente no coincide con el asiento generado.");
            }

            cheque.AsientoContableRechazoId = asiento.AsientoContableId;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetChequeAsync(id))!;
        }

        public async Task EnsureChequesDesdeCobranzaConfirmadaAsync(Cobranza cobranza)
        {
            var mediosCheque = cobranza.MediosPago
                .Where(m => string.Equals(m.MedioPago.Codigo, CodigoMedioCheque, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!mediosCheque.Any()) return;

            var medioIds = mediosCheque.Select(m => m.Id).ToList();
            var existentes = await _db.ChequesTerceros
                .Where(c => medioIds.Contains(c.CobranzaMedioPagoId))
                .Select(c => c.CobranzaMedioPagoId)
                .ToListAsync();
            var existentesSet = existentes.ToHashSet();

            foreach (var medio in mediosCheque)
            {
                if (existentesSet.Contains(medio.Id)) continue;
                ValidateMedioChequeParaCartera(medio);

                _db.ChequesTerceros.Add(new ChequeTercero
                {
                    CobranzaMedioPagoId = medio.Id,
                    BancoCobranzaId = medio.BancoCobranzaId!.Value,
                    NumeroCheque = medio.NumeroReferencia!.Trim(),
                    FechaEmision = NormalizeRequiredDate(medio.FechaEmision!.Value, "La fecha de emision del cheque es obligatoria."),
                    FechaVencimiento = NormalizeRequiredDate(medio.FechaValor!.Value, "La fecha de vencimiento del cheque es obligatoria."),
                    Importe = RoundMoney(medio.Importe),
                    MonedaCodigo = NormalizeCurrency(cobranza.MonedaCodigo),
                    Librador = medio.Librador!.Trim(),
                    CuitLibrador = medio.CuitLibrador!.Trim(),
                    Estado = ChequeTerceroEstado.EN_CARTERA,
                    Observaciones = medio.Observaciones,
                    FechaAlta = DateTime.UtcNow,
                    UsuarioAlta = _userContext.UserName
                });
            }
        }

        private IQueryable<ChequeTercero> GetChequeQuery(bool tracking)
        {
            var query = _db.ChequesTerceros
                .Include(c => c.BancoCatalogo)
                .Include(c => c.CuentaBancariaEmpresa)
                    .ThenInclude(c => c!.Banco)
                .Include(c => c.CuentaBancariaEmpresa)
                    .ThenInclude(c => c!.CuentaContable)
                .Include(c => c.CobranzaMedioPago)
                    .ThenInclude(m => m.MedioPago)
                .Include(c => c.CobranzaMedioPago)
                    .ThenInclude(m => m.Cobranza)
                .AsQueryable();

            return tracking ? query : query.AsNoTracking();
        }

        private static void ValidateMedioChequeParaCartera(CobranzaMedioPago medio)
        {
            if (!medio.BancoCobranzaId.HasValue) throw new InvalidOperationException("El cheque requiere banco.");
            if (string.IsNullOrWhiteSpace(medio.NumeroReferencia)) throw new InvalidOperationException("El cheque requiere numero.");
            if (!medio.FechaEmision.HasValue) throw new InvalidOperationException("El cheque requiere fecha de emision.");
            if (!medio.FechaValor.HasValue) throw new InvalidOperationException("El cheque requiere fecha de vencimiento.");
            if (medio.Importe <= 0) throw new InvalidOperationException("El importe del cheque debe ser mayor a cero.");
            if (string.IsNullOrWhiteSpace(medio.Librador)) throw new InvalidOperationException("El cheque requiere librador.");
            if (string.IsNullOrWhiteSpace(medio.CuitLibrador)) throw new InvalidOperationException("El cheque requiere CUIT del librador.");
        }

        private static ChequeTerceroListResponse MapList(ChequeTercero cheque)
        {
            return new ChequeTerceroListResponse
            {
                Id = cheque.Id,
                NumeroCheque = cheque.NumeroCheque,
                BancoCobranzaId = cheque.BancoCobranzaId,
                Banco = cheque.BancoCatalogo.Nombre,
                FechaVencimiento = cheque.FechaVencimiento,
                Importe = cheque.Importe,
                MonedaCodigo = cheque.MonedaCodigo,
                Librador = cheque.Librador,
                Estado = cheque.Estado,
                ClienteExternoId = cheque.CobranzaMedioPago.Cobranza.ClienteExternoId,
                CobranzaId = cheque.CobranzaMedioPago.CobranzaId
            };
        }

        private static ChequeTerceroDetalleResponse MapDetalle(ChequeTercero cheque)
        {
            var list = MapList(cheque);
            return new ChequeTerceroDetalleResponse
            {
                Id = list.Id,
                NumeroCheque = list.NumeroCheque,
                BancoCobranzaId = list.BancoCobranzaId,
                Banco = list.Banco,
                FechaVencimiento = list.FechaVencimiento,
                Importe = list.Importe,
                MonedaCodigo = list.MonedaCodigo,
                Librador = list.Librador,
                Estado = list.Estado,
                ClienteExternoId = list.ClienteExternoId,
                CobranzaId = list.CobranzaId,
                CobranzaMedioPagoId = cheque.CobranzaMedioPagoId,
                FechaEmision = cheque.FechaEmision,
                CuitLibrador = cheque.CuitLibrador,
                Observaciones = cheque.Observaciones,
                FechaAlta = cheque.FechaAlta,
                UsuarioAlta = cheque.UsuarioAlta,
                FechaModificacion = cheque.FechaModificacion,
                UsuarioModificacion = cheque.UsuarioModificacion,
                FechaDeposito = cheque.FechaDeposito,
                CuentaBancariaEmpresaId = cheque.CuentaBancariaEmpresaId,
                CuentaBancariaEmpresaDescripcion = cheque.CuentaBancariaEmpresa?.Descripcion,
                CuentaBancariaEmpresaBanco = cheque.CuentaBancariaEmpresa?.Banco.Nombre,
                CuentaBancariaEmpresaTipoCuenta = cheque.CuentaBancariaEmpresa?.TipoCuenta,
                CuentaBancariaEmpresaNumeroCuenta = cheque.CuentaBancariaEmpresa?.NumeroCuenta,
                CuentaBancariaEmpresaMonedaCodigo = cheque.CuentaBancariaEmpresa?.MonedaCodigo,
                BancoDestino = cheque.BancoDestino,
                CuentaDestino = cheque.CuentaDestino,
                UsuarioDeposito = cheque.UsuarioDeposito,
                FechaAcreditacion = cheque.FechaAcreditacion,
                UsuarioAcreditacion = cheque.UsuarioAcreditacion,
                MedioPagoCodigo = cheque.CobranzaMedioPago.MedioPago.Codigo,
                MedioPagoDescripcion = cheque.CobranzaMedioPago.MedioPago.Descripcion,
                FechaRechazo = cheque.FechaRechazo,
                MotivoRechazo = cheque.MotivoRechazo,
                UsuarioRechazo = cheque.UsuarioRechazo,
                AsientoContableAcreditacionId = cheque.AsientoContableAcreditacionId,
                AsientoContableRechazoId = cheque.AsientoContableRechazoId
            };
        }

        private async Task<CuentaBancariaEmpresa> GetCuentaBancariaDepositoAsync(int cuentaBancariaEmpresaId, string chequeMoneda)
        {
            var cuenta = await _db.CuentasBancariasEmpresa
                .Include(c => c.Banco)
                .Include(c => c.CuentaContable)
                .FirstOrDefaultAsync(c => c.Id == cuentaBancariaEmpresaId);

            if (cuenta == null)
            {
                throw new InvalidOperationException("La cuenta bancaria destino no existe.");
            }

            if (!cuenta.Activa)
            {
                throw new InvalidOperationException("La cuenta bancaria destino debe estar activa.");
            }

            if (!cuenta.Banco.Activo)
            {
                throw new InvalidOperationException("El banco de la cuenta bancaria destino debe estar activo.");
            }

            if (!string.Equals(NormalizeCurrency(chequeMoneda), NormalizeCurrency(cuenta.MonedaCodigo), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La moneda del cheque debe coincidir con la moneda de la cuenta bancaria destino.");
            }

            if (cuenta.CuentaContableId <= 0 || cuenta.CuentaContable == null)
            {
                throw new InvalidOperationException("La cuenta bancaria destino no tiene cuenta contable configurada.");
            }

            if (!cuenta.CuentaContable.Activa)
            {
                throw new InvalidOperationException("La cuenta contable de la cuenta bancaria destino debe estar activa.");
            }

            return cuenta;
        }

        private async Task<Cobranza> GetCobranzaOrigenParaRechazoAsync(int cobranzaId)
        {
            var cobranza = await _db.Cobranzas
                .Include(c => c.MediosPago)
                    .ThenInclude(m => m.AplicacionesFactura)
                .Include(c => c.MediosPago)
                    .ThenInclude(m => m.ChequeTercero)
                .Include(c => c.AplicacionesFactura)
                    .ThenInclude(a => a.Venta)
                .Include(c => c.AplicacionesFactura)
                    .ThenInclude(a => a.AplicacionesMediosPago)
                .Include(c => c.AplicacionesFactura)
                    .ThenInclude(a => a.AplicacionesObligacion)
                        .ThenInclude(o => o.CuotaComercial)
                .FirstOrDefaultAsync(c => c.Id == cobranzaId);

            return cobranza ?? throw new InvalidOperationException("Cobranza origen no encontrada.");
        }

        private async Task EnsureDistribucionMediosAplicacionesFacturaAsync(Cobranza cobranza)
        {
            var medios = cobranza.MediosPago.OrderBy(m => m.Id).ToList();
            var aplicaciones = cobranza.AplicacionesFactura.OrderBy(a => a.Id).ToList();
            if (!medios.Any() || !aplicaciones.Any()) return;

            var medioIds = medios.Select(m => m.Id).ToList();
            var aplicacionIds = aplicaciones.Select(a => a.Id).ToList();
            var existentes = await _db.CobranzasMediosPagoAplicacionesFactura
                .Where(d => medioIds.Contains(d.CobranzaMedioPagoId) || aplicacionIds.Contains(d.CobranzaAplicacionFacturaId))
                .ToListAsync();

            if (existentes.Any())
            {
                ValidateDistribucionCompleta(cobranza, existentes);
                return;
            }

            var restanteAplicaciones = aplicaciones.ToDictionary(a => a.Id, a => RoundMoney(a.ImporteAplicado));
            var aplicacionIndex = 0;
            var now = DateTime.UtcNow;

            foreach (var medio in medios)
            {
                var restanteMedio = RoundMoney(medio.Importe);
                while (restanteMedio > 0 && aplicacionIndex < aplicaciones.Count)
                {
                    var aplicacion = aplicaciones[aplicacionIndex];
                    var restanteAplicacion = restanteAplicaciones[aplicacion.Id];
                    if (restanteAplicacion <= 0)
                    {
                        aplicacionIndex++;
                        continue;
                    }

                    var importe = RoundMoney(Math.Min(restanteMedio, restanteAplicacion));
                    _db.CobranzasMediosPagoAplicacionesFactura.Add(new CobranzaMedioPagoAplicacionFactura
                    {
                        CobranzaMedioPagoId = medio.Id,
                        CobranzaAplicacionFacturaId = aplicacion.Id,
                        ImporteAplicado = importe,
                        FechaAlta = now,
                        UsuarioAlta = _userContext.UserName
                    });

                    restanteMedio = RoundMoney(restanteMedio - importe);
                    restanteAplicaciones[aplicacion.Id] = RoundMoney(restanteAplicacion - importe);
                }

                if (restanteMedio > 0)
                {
                    throw new InvalidOperationException("No se pudo distribuir completamente los medios de pago de la cobranza.");
                }
            }

            if (restanteAplicaciones.Values.Any(v => v > 0))
            {
                throw new InvalidOperationException("No se pudo distribuir completamente las aplicaciones de factura de la cobranza.");
            }

            await _db.SaveChangesAsync();
        }

        private static void ValidateDistribucionCompleta(Cobranza cobranza, IEnumerable<CobranzaMedioPagoAplicacionFactura> distribuciones)
        {
            var medios = cobranza.MediosPago.ToDictionary(m => m.Id);
            var aplicaciones = cobranza.AplicacionesFactura.ToDictionary(a => a.Id);

            foreach (var distribucion in distribuciones)
            {
                if (!medios.TryGetValue(distribucion.CobranzaMedioPagoId, out var medio) ||
                    !aplicaciones.TryGetValue(distribucion.CobranzaAplicacionFacturaId, out var aplicacion) ||
                    medio.CobranzaId != cobranza.Id ||
                    aplicacion.CobranzaId != cobranza.Id)
                {
                    throw new InvalidOperationException("La distribucion de medios de pago no pertenece completamente a la cobranza.");
                }
            }

            foreach (var medio in medios.Values)
            {
                var distribuido = RoundMoney(distribuciones.Where(d => d.CobranzaMedioPagoId == medio.Id).Sum(d => d.ImporteAplicado));
                if (distribuido != RoundMoney(medio.Importe))
                {
                    throw new InvalidOperationException("La distribucion de medios de pago de la cobranza esta incompleta o inconsistente.");
                }
            }

            foreach (var aplicacion in aplicaciones.Values)
            {
                var distribuido = RoundMoney(distribuciones.Where(d => d.CobranzaAplicacionFacturaId == aplicacion.Id).Sum(d => d.ImporteAplicado));
                if (distribuido != RoundMoney(aplicacion.ImporteAplicado))
                {
                    throw new InvalidOperationException("La distribucion de aplicaciones de factura de la cobranza esta incompleta o inconsistente.");
                }
            }
        }

        private async Task EnsureMovimientosRechazoCuentaCorrienteAsync(
            ChequeTercero cheque,
            IEnumerable<CobranzaMedioPagoAplicacionFactura> distribuciones,
            DateTime fechaRechazo,
            DateTime fechaAlta)
        {
            var porFactura = distribuciones
                .GroupBy(d => d.AplicacionFactura)
                .Select(g => new { Aplicacion = g.Key, Importe = RoundMoney(g.Sum(d => d.ImporteAplicado)) })
                .ToList();

            foreach (var item in porFactura)
            {
                var tipoMovimiento = BuildTipoMovimientoRechazoCheque(item.Aplicacion.VentaId);
                var idOrigen = cheque.Id.ToString();
                var existing = await _db.VentasMovimientosCuentaCorriente.FirstOrDefaultAsync(m =>
                    m.ModuloOrigen == ModuloOrigenCarteraCheques &&
                    m.IdOrigen == idOrigen &&
                    m.TipoMovimiento == tipoMovimiento);

                if (existing != null)
                {
                    if (existing.Debe != item.Importe ||
                        existing.Haber != 0 ||
                        existing.ClienteExternoId != cheque.CobranzaMedioPago.Cobranza.ClienteExternoId ||
                        existing.ObraExternaId != item.Aplicacion.Venta.ObraExternaId)
                    {
                        throw new InvalidOperationException("Existe un movimiento de cuenta corriente de rechazo inconsistente.");
                    }

                    continue;
                }

                _db.VentasMovimientosCuentaCorriente.Add(new VentaMovimientoCuentaCorriente
                {
                    ClienteExternoId = cheque.CobranzaMedioPago.Cobranza.ClienteExternoId,
                    ObraExternaId = item.Aplicacion.Venta.ObraExternaId,
                    Fecha = fechaRechazo,
                    TipoMovimiento = tipoMovimiento,
                    Debe = item.Importe,
                    Haber = 0,
                    ModuloOrigen = ModuloOrigenCarteraCheques,
                    IdOrigen = idOrigen,
                    Descripcion = $"Rechazo cheque {cheque.NumeroCheque} aplicado a factura {BuildComprobante(item.Aplicacion.Venta)}",
                    FechaAlta = fechaAlta,
                    UsuarioAlta = _userContext.UserName
                });

                await _db.SaveChangesAsync();
            }
        }

        private async Task RecalcularCuotasAfectadasPorRechazosAsync(IEnumerable<CobranzaMedioPagoAplicacionFactura> distribucionesCheque)
        {
            var aplicacionIds = distribucionesCheque.Select(d => d.CobranzaAplicacionFacturaId).Distinct().ToList();
            var cuotaIds = await _db.CobranzasAplicacionesObligacion
                .Where(o => aplicacionIds.Contains(o.CobranzaAplicacionFacturaId))
                .Select(o => o.CuotaComercialId)
                .Distinct()
                .ToListAsync();

            if (!cuotaIds.Any()) return;

            var importesConfirmados = await _db.CobranzasAplicacionesObligacion
                .AsNoTracking()
                .Where(o => cuotaIds.Contains(o.CuotaComercialId) &&
                    o.AplicacionFactura.Cobranza.Estado == CobranzaEstado.Confirmada)
                .GroupBy(o => o.CuotaComercialId)
                .Select(g => new { CuotaId = g.Key, Importe = g.Sum(o => o.ImporteAplicado) })
                .ToDictionaryAsync(g => g.CuotaId, g => g.Importe);

            var importesRechazados = await BuildImportesRechazadosPorCuotaAsync(cuotaIds);
            var cuotas = await _db.CuotasComerciales.Where(c => cuotaIds.Contains(c.Id)).ToListAsync();

            foreach (var cuota in cuotas)
            {
                importesConfirmados.TryGetValue(cuota.Id, out var confirmado);
                importesRechazados.TryGetValue(cuota.Id, out var rechazado);
                cuota.ImportePagado = Math.Max(RoundMoney(confirmado - rechazado), 0);
                cuota.SaldoPendiente = Math.Max(RoundMoney(cuota.ImporteOriginal - cuota.ImportePagado), 0);
                UpdateCuotaEstado(cuota);
            }

            await _db.SaveChangesAsync();
        }

        private async Task<Dictionary<int, decimal>> BuildImportesRechazadosPorCuotaAsync(IEnumerable<int> cuotaIds)
        {
            var cuotaIdList = cuotaIds.Distinct().ToList();
            var distribucionesRechazadas = await _db.CobranzasMediosPagoAplicacionesFactura
                .AsNoTracking()
                .Include(d => d.CobranzaMedioPago)
                    .ThenInclude(m => m.ChequeTercero)
                .Include(d => d.AplicacionFactura)
                    .ThenInclude(a => a.AplicacionesObligacion)
                .Where(d => d.CobranzaMedioPago.ChequeTercero != null &&
                    d.CobranzaMedioPago.ChequeTercero.Estado == ChequeTerceroEstado.RECHAZADO &&
                    d.AplicacionFactura.AplicacionesObligacion.Any(o => cuotaIdList.Contains(o.CuotaComercialId)))
                .ToListAsync();

            var cuotaIdSet = cuotaIdList.ToHashSet();
            var result = new Dictionary<int, decimal>();
            foreach (var distribucion in distribucionesRechazadas)
            {
                foreach (var item in DistribuirImporteEnObligaciones(distribucion.ImporteAplicado, distribucion.AplicacionFactura.AplicacionesObligacion))
                {
                    if (!cuotaIdSet.Contains(item.CuotaComercialId)) continue;
                    result[item.CuotaComercialId] = RoundMoney(result.GetValueOrDefault(item.CuotaComercialId) + item.Importe);
                }
            }

            return result;
        }

        private static IEnumerable<ImportePorCuota> DistribuirImporteEnObligaciones(
            decimal importeFacturaRechazado,
            IEnumerable<CobranzaAplicacionObligacion> obligaciones)
        {
            var restante = RoundMoney(importeFacturaRechazado);
            foreach (var obligacion in obligaciones.OrderBy(o => o.Id))
            {
                if (restante <= 0) yield break;
                var importe = RoundMoney(Math.Min(restante, obligacion.ImporteAplicado));
                if (importe <= 0) continue;
                yield return new ImportePorCuota(obligacion.CuotaComercialId, importe);
                restante = RoundMoney(restante - importe);
            }
        }

        private static SolicitudContabilizacionAutomaticaRequest BuildSolicitudContableRechazo(
            ChequeTercero cheque,
            DateTime fechaRechazo,
            string motivo)
        {
            return new SolicitudContabilizacionAutomaticaRequest
            {
                CodigoOperacion = CodigoOperacionRechazoChequeCliente,
                ModuloOrigen = ModuloOrigenCarteraCheques,
                IdOrigen = cheque.Id.ToString(),
                Fecha = fechaRechazo,
                Descripcion = $"Rechazo cheque {cheque.NumeroCheque}: {motivo}",
                ImportesPorConcepto = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    [ConceptoClientes] = RoundMoney(cheque.Importe),
                    [ConceptoChequesTerceros] = RoundMoney(cheque.Importe)
                }
            };
        }

        private static SolicitudContabilizacionAutomaticaRequest BuildSolicitudContableAcreditacion(
            ChequeTercero cheque,
            CuentaBancariaEmpresa cuentaBancaria,
            DateTime fechaAcreditacion)
        {
            return new SolicitudContabilizacionAutomaticaRequest
            {
                CodigoOperacion = CodigoOperacionAcreditacionCheque,
                ModuloOrigen = ModuloOrigenCarteraCheques,
                IdOrigen = cheque.Id.ToString(),
                Fecha = fechaAcreditacion,
                Descripcion = $"Acreditacion cheque {cheque.NumeroCheque} en {cuentaBancaria.Banco.Nombre} {cuentaBancaria.Descripcion}",
                ImportesPorConcepto = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    [ConceptoBanco] = RoundMoney(cheque.Importe),
                    [ConceptoChequesTerceros] = RoundMoney(cheque.Importe)
                },
                CuentasContablesOverridePorConcepto = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    [ConceptoBanco] = cuentaBancaria.CuentaContableId
                }
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

            cuota.Estado = cuota.ImportePagado > 0 ? CuotaEstado.Parcial : CuotaEstado.Pendiente;
        }

        private static string BuildTipoMovimientoRechazoCheque(int ventaId)
        {
            return $"RECHAZO_CHEQUE:{ventaId}";
        }

        private static string BuildComprobante(Venta venta)
        {
            return $"{venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}";
        }

        private static DateTime NormalizeRequiredDate(DateTime value, string errorMessage)
        {
            if (value == default) throw new InvalidOperationException(errorMessage);
            return NormalizeDateOnlyUtc(value);
        }

        private static DateTime NormalizeDateOnlyUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
        }

        private static string NormalizeRequired(string? value, string errorMessage)
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException(errorMessage);
            return normalized;
        }

        private static string NormalizeCurrency(string? value)
        {
            var normalized = NormalizeRequired(value, "La moneda es obligatoria.").ToUpperInvariant();
            if (normalized.Length > 10) throw new InvalidOperationException("La moneda no es valida.");
            return normalized;
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private sealed record ImportePorCuota(int CuotaComercialId, decimal Importe);
    }
}

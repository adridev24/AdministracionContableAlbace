using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Sales
{
    public class CuentaCorrienteClientesService : ICuentaCorrienteClientesService
    {
        private const string ModuloOrigenVentas = "VENTAS";
        private const string ModuloOrigenCobranzas = "COBRANZAS";
        private const string EstadoPendiente = "PENDIENTE";
        private const string EstadoParcial = "PARCIALMENTE_COBRADA";
        private const string EstadoCancelada = "CANCELADA";

        private readonly AppDbContext _db;
        private readonly IExternalDataService _externalDataService;

        public CuentaCorrienteClientesService(AppDbContext db, IExternalDataService externalDataService)
        {
            _db = db;
            _externalDataService = externalDataService;
        }

        public async Task<CuentaCorrienteClienteResponse> GetCuentaCorrienteAsync(string clienteId, CuentaCorrienteClienteFilterRequest filter)
        {
            var normalizedClienteId = NormalizeRequiredId(clienteId, "Debe indicar un cliente.");
            var normalizedObraId = NormalizeOptionalId(filter.ObraId);
            var normalizedMoneda = NormalizeOptionalCurrency(filter.Moneda);
            var normalizedEstadoFactura = NormalizeOptionalEstadoFactura(filter.EstadoFactura);
            var fechaDesde = filter.FechaDesde.HasValue ? NormalizeDateOnlyUtc(filter.FechaDesde.Value) : (DateTime?)null;
            var fechaHastaExclusive = filter.FechaHasta.HasValue ? NormalizeDateOnlyUtc(filter.FechaHasta.Value).AddDays(1) : (DateTime?)null;

            if (fechaDesde.HasValue && fechaHastaExclusive.HasValue && fechaDesde.Value >= fechaHastaExclusive.Value)
            {
                throw new InvalidOperationException("La fecha desde no puede ser posterior a la fecha hasta.");
            }

            var cliente = await GetClienteAsync(normalizedClienteId);
            var movimientosBase = await GetMovimientosBaseAsync(normalizedClienteId, normalizedObraId, fechaHastaExclusive);
            var ventaIdsOrigen = movimientosBase
                .Where(m => IsModulo(m.ModuloOrigen, ModuloOrigenVentas))
                .Select(m => TryParseInt(m.IdOrigen))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            var cobranzaIdsOrigen = movimientosBase
                .Where(m => IsModulo(m.ModuloOrigen, ModuloOrigenCobranzas))
                .Select(m => TryParseInt(m.IdOrigen))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var ventasOrigen = await _db.Ventas
                .AsNoTracking()
                .Include(v => v.TipoComprobante)
                .Where(v => ventaIdsOrigen.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            var cobranzasOrigen = await _db.Cobranzas
                .AsNoTracking()
                .Where(c => cobranzaIdsOrigen.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            var obraIds = movimientosBase.Select(m => m.ObraExternaId)
                .Concat(ventasOrigen.Values.Select(v => v.ObraExternaId))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            var obras = await GetObrasAsync(normalizedClienteId, obraIds);

            var cuentaMovimientos = BuildMovimientos(movimientosBase, ventasOrigen, cobranzasOrigen, obras, normalizedMoneda, fechaDesde);

            var facturas = await BuildFacturasAsync(normalizedClienteId, normalizedObraId, normalizedMoneda, normalizedEstadoFactura, fechaDesde, fechaHastaExclusive, obras);

            if (cliente == null && !cuentaMovimientos.Movimientos.Any() && !cuentaMovimientos.Saldos.Any() && !facturas.Any())
            {
                throw new InvalidOperationException("Cliente inexistente o sin movimientos de cuenta corriente.");
            }

            return new CuentaCorrienteClienteResponse
            {
                ClienteId = normalizedClienteId,
                ClienteNombre = cliente?.NombreCliente,
                SaldosPorMoneda = cuentaMovimientos.Saldos,
                Movimientos = cuentaMovimientos.Movimientos,
                Facturas = facturas
            };
        }

        private async Task<List<VentaMovimientoCuentaCorriente>> GetMovimientosBaseAsync(
            string clienteId,
            string? obraId,
            DateTime? fechaHastaExclusive)
        {
            var query = _db.VentasMovimientosCuentaCorriente
                .AsNoTracking()
                .Where(m => m.ClienteExternoId == clienteId);

            if (!string.IsNullOrWhiteSpace(obraId))
            {
                query = query.Where(m => m.ObraExternaId == obraId);
            }

            if (fechaHastaExclusive.HasValue)
            {
                query = query.Where(m => m.Fecha < fechaHastaExclusive.Value);
            }

            return await query.OrderBy(m => m.Fecha).ThenBy(m => m.Id).ToListAsync();
        }

        private static CuentaCorrienteMovimientosResult BuildMovimientos(
            IEnumerable<VentaMovimientoCuentaCorriente> movimientosBase,
            IReadOnlyDictionary<int, Venta> ventasOrigen,
            IReadOnlyDictionary<int, Cobranza> cobranzasOrigen,
            IReadOnlyDictionary<string, Obra> obras,
            string? monedaFiltro,
            DateTime? fechaDesde)
        {
            var movimientosResueltos = movimientosBase
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.Id)
                .Select(m => ResolveMovimiento(m, ventasOrigen, cobranzasOrigen, obras))
                .Where(m => m != null)
                .Select(m => m!)
                .Where(m => string.IsNullOrWhiteSpace(monedaFiltro) || string.Equals(m.MonedaCodigo, monedaFiltro, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var movimientosAnteriores = fechaDesde.HasValue
                ? movimientosResueltos.Where(m => m.Movimiento.Fecha < fechaDesde.Value).ToList()
                : new List<MovimientoCuentaCorrienteCalculado>();
            var movimientosPeriodo = fechaDesde.HasValue
                ? movimientosResueltos.Where(m => m.Movimiento.Fecha >= fechaDesde.Value).ToList()
                : movimientosResueltos;

            var saldosAnteriores = movimientosAnteriores
                .GroupBy(m => m.MonedaCodigo)
                .ToDictionary(g => g.Key, g => RoundMoney(g.Sum(m => m.Movimiento.Debe - m.Movimiento.Haber)), StringComparer.OrdinalIgnoreCase);

            var saldosAcumulados = saldosAnteriores.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            var movimientos = new List<CuentaCorrienteMovimientoResponse>();
            foreach (var item in movimientosPeriodo)
            {
                var movimiento = item.Movimiento;
                saldosAcumulados.TryGetValue(item.MonedaCodigo, out var saldoAnterior);
                var saldo = RoundMoney(saldoAnterior + movimiento.Debe - movimiento.Haber);
                saldosAcumulados[item.MonedaCodigo] = saldo;

                movimientos.Add(new CuentaCorrienteMovimientoResponse
                {
                    Id = movimiento.Id,
                    Fecha = movimiento.Fecha,
                    TipoMovimiento = movimiento.TipoMovimiento,
                    Descripcion = movimiento.Descripcion,
                    Debe = RoundMoney(movimiento.Debe),
                    Haber = RoundMoney(movimiento.Haber),
                    SaldoAcumulado = saldo,
                    MonedaCodigo = item.MonedaCodigo,
                    ObraId = item.ObraId,
                    ObraNombre = item.ObraNombre,
                    ModuloOrigen = movimiento.ModuloOrigen,
                    IdOrigen = movimiento.IdOrigen,
                    NumeroComprobante = item.NumeroComprobante
                });
            }

            var monedas = movimientosResueltos
                .Select(m => m.MonedaCodigo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();

            var saldos = monedas.Select(moneda =>
            {
                var saldoAnterior = saldosAnteriores.GetValueOrDefault(moneda);
                var debePeriodo = RoundMoney(movimientosPeriodo
                    .Where(m => string.Equals(m.MonedaCodigo, moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Movimiento.Debe));
                var haberPeriodo = RoundMoney(movimientosPeriodo
                    .Where(m => string.Equals(m.MonedaCodigo, moneda, StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Movimiento.Haber));
                var saldoFinal = RoundMoney(saldoAnterior + debePeriodo - haberPeriodo);
                return new CuentaCorrienteSaldoMonedaResponse
                {
                    MonedaCodigo = moneda,
                    Debe = debePeriodo,
                    Haber = haberPeriodo,
                    Saldo = saldoFinal,
                    SaldoAnterior = saldoAnterior,
                    DebePeriodo = debePeriodo,
                    HaberPeriodo = haberPeriodo,
                    SaldoFinal = saldoFinal
                };
            }).ToList();

            return new CuentaCorrienteMovimientosResult(saldos, movimientos);
        }

        private static MovimientoCuentaCorrienteCalculado? ResolveMovimiento(
            VentaMovimientoCuentaCorriente movimiento,
            IReadOnlyDictionary<int, Venta> ventasOrigen,
            IReadOnlyDictionary<int, Cobranza> cobranzasOrigen,
            IReadOnlyDictionary<string, Obra> obras)
        {
            var moneda = ResolveMoneda(movimiento, ventasOrigen, cobranzasOrigen);
            if (string.IsNullOrWhiteSpace(moneda)) return null;

            var obraId = ResolveObraId(movimiento, ventasOrigen);
            obras.TryGetValue(obraId, out var obra);
            return new MovimientoCuentaCorrienteCalculado(
                movimiento,
                moneda,
                obraId,
                obra?.NombreObra,
                ResolveNumeroComprobante(movimiento, ventasOrigen));
        }

        private async Task<List<CuentaCorrienteFacturaResponse>> BuildFacturasAsync(
            string clienteId,
            string? obraId,
            string? moneda,
            string? estadoFactura,
            DateTime? fechaDesde,
            DateTime? fechaHastaExclusive,
            IReadOnlyDictionary<string, Obra> obrasIniciales)
        {
            var query = _db.Ventas
                .AsNoTracking()
                .Include(v => v.TipoComprobante)
                .Where(v => v.ClienteExternoId == clienteId && v.Estado == VentaEstado.Confirmada);

            if (!string.IsNullOrWhiteSpace(obraId))
            {
                query = query.Where(v => v.ObraExternaId == obraId);
            }

            if (!string.IsNullOrWhiteSpace(moneda))
            {
                query = query.Where(v => v.MonedaCodigo == moneda);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(v => v.FechaComprobante >= fechaDesde.Value);
            }

            if (fechaHastaExclusive.HasValue)
            {
                query = query.Where(v => v.FechaComprobante < fechaHastaExclusive.Value);
            }

            var ventas = await query.OrderBy(v => v.FechaComprobante).ThenBy(v => v.Id).ToListAsync();
            var ventaIds = ventas.Select(v => v.Id).ToList();
            if (!ventaIds.Any()) return new List<CuentaCorrienteFacturaResponse>();

            var aplicaciones = await _db.CobranzasAplicacionesFactura
                .AsNoTracking()
                .Include(a => a.Cobranza)
                .Where(a => ventaIds.Contains(a.VentaId))
                .ToListAsync();

            var obraIdsFaltantes = ventas
                .Select(v => v.ObraExternaId)
                .Where(id => !obrasIniciales.ContainsKey(id))
                .Distinct()
                .ToList();
            var obras = obrasIniciales.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var obra in await GetObrasAsync(clienteId, obraIdsFaltantes))
            {
                obras[obra.Key] = obra.Value;
            }

            var result = ventas.Select(v =>
            {
                var aplicacionesFactura = aplicaciones
                    .Where(a => a.VentaId == v.Id)
                    .OrderBy(a => a.Cobranza.Fecha)
                    .ThenBy(a => a.CobranzaId)
                    .ToList();
                var totalCobrado = RoundMoney(aplicacionesFactura
                    .Where(a => a.Cobranza.Estado == CobranzaEstado.Confirmada)
                    .Sum(a => a.ImporteAplicado));
                var saldo = RoundMoney(v.Total - totalCobrado);
                var estado = BuildEstadoFactura(v.Total, totalCobrado);
                obras.TryGetValue(v.ObraExternaId, out var obra);

                return new CuentaCorrienteFacturaResponse
                {
                    VentaId = v.Id,
                    Fecha = v.FechaComprobante,
                    TipoComprobante = v.TipoComprobante?.Descripcion ?? v.TipoComprobante?.Codigo ?? string.Empty,
                    NumeroComprobante = BuildComprobante(v),
                    ObraId = v.ObraExternaId,
                    ObraNombre = obra?.NombreObra,
                    MonedaCodigo = v.MonedaCodigo,
                    TotalFactura = RoundMoney(v.Total),
                    TotalCobrado = totalCobrado,
                    Saldo = saldo,
                    EstadoCobranza = estado,
                    Cobranzas = aplicacionesFactura.Select(a => new CuentaCorrienteCobranzaAplicadaResponse
                    {
                        CobranzaId = a.CobranzaId,
                        Fecha = a.Cobranza.Fecha,
                        ImporteAplicado = RoundMoney(a.ImporteAplicado),
                        EstadoCobranza = a.Cobranza.Estado.ToString()
                    }).ToList()
                };
            });

            if (!string.IsNullOrWhiteSpace(estadoFactura))
            {
                result = result.Where(f => f.EstadoCobranza == estadoFactura);
            }

            return result.ToList();
        }

        private async Task<Client?> GetClienteAsync(string clienteId)
        {
            return int.TryParse(clienteId, out var id) ? await _externalDataService.GetClientByIdAsync(id) : null;
        }

        private async Task<Dictionary<string, Obra>> GetObrasAsync(string clienteId, IEnumerable<string> obraIds)
        {
            var ids = obraIds
                .Select(TryParseInt)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            if (!ids.Any()) return new Dictionary<string, Obra>(StringComparer.OrdinalIgnoreCase);

            var obras = int.TryParse(clienteId, out var parsedClienteId)
                ? await _externalDataService.GetObrasByClientAsync(parsedClienteId)
                : await _externalDataService.GetObrasAsync();

            return obras
                .Where(o => ids.Contains(o.IdObra))
                .ToDictionary(o => o.IdObra.ToString(), o => o, StringComparer.OrdinalIgnoreCase);
        }

        private static string? ResolveMoneda(
            VentaMovimientoCuentaCorriente movimiento,
            IReadOnlyDictionary<int, Venta> ventasOrigen,
            IReadOnlyDictionary<int, Cobranza> cobranzasOrigen)
        {
            var id = TryParseInt(movimiento.IdOrigen);
            if (!id.HasValue) return null;

            if (IsModulo(movimiento.ModuloOrigen, ModuloOrigenVentas) && ventasOrigen.TryGetValue(id.Value, out var venta))
            {
                return venta.MonedaCodigo;
            }

            if (IsModulo(movimiento.ModuloOrigen, ModuloOrigenCobranzas) && cobranzasOrigen.TryGetValue(id.Value, out var cobranza))
            {
                return cobranza.MonedaCodigo;
            }

            return null;
        }

        private static string ResolveObraId(VentaMovimientoCuentaCorriente movimiento, IReadOnlyDictionary<int, Venta> ventasOrigen)
        {
            var id = TryParseInt(movimiento.IdOrigen);
            if (id.HasValue && IsModulo(movimiento.ModuloOrigen, ModuloOrigenVentas) && ventasOrigen.TryGetValue(id.Value, out var venta))
            {
                return venta.ObraExternaId;
            }

            return movimiento.ObraExternaId;
        }

        private static string? ResolveNumeroComprobante(VentaMovimientoCuentaCorriente movimiento, IReadOnlyDictionary<int, Venta> ventasOrigen)
        {
            var id = TryParseInt(movimiento.IdOrigen);
            if (id.HasValue && IsModulo(movimiento.ModuloOrigen, ModuloOrigenVentas) && ventasOrigen.TryGetValue(id.Value, out var venta))
            {
                return BuildComprobante(venta);
            }

            if (IsModulo(movimiento.ModuloOrigen, ModuloOrigenCobranzas))
            {
                return $"Cobranza {movimiento.IdOrigen}";
            }

            return null;
        }

        private static string BuildEstadoFactura(decimal totalFactura, decimal totalCobrado)
        {
            if (totalCobrado <= 0) return EstadoPendiente;
            return totalCobrado >= RoundMoney(totalFactura) ? EstadoCancelada : EstadoParcial;
        }

        private static string BuildComprobante(Venta venta)
        {
            return $"{venta.TipoComprobante?.Codigo ?? "Factura"} {venta.PuntoVenta:0000}-{venta.NumeroComprobante:00000000}";
        }

        private static string NormalizeRequiredId(string value, string errorMessage)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException(errorMessage);
            return normalized;
        }

        private static string? NormalizeOptionalId(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string? NormalizeOptionalCurrency(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            if (normalized.Length > 10) throw new InvalidOperationException("La moneda indicada no es valida.");
            return normalized;
        }

        private static string? NormalizeOptionalEstadoFactura(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized)) return null;
            var allowed = new[] { EstadoPendiente, EstadoParcial, EstadoCancelada };
            if (!allowed.Contains(normalized)) throw new InvalidOperationException("El estado de factura indicado no es valido.");
            return normalized;
        }

        private static bool IsModulo(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static int? TryParseInt(string value)
        {
            return int.TryParse(value, out var id) ? id : null;
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime NormalizeDateOnlyUtc(DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed record CuentaCorrienteMovimientosResult(
            List<CuentaCorrienteSaldoMonedaResponse> Saldos,
            List<CuentaCorrienteMovimientoResponse> Movimientos);

        private sealed record MovimientoCuentaCorrienteCalculado(
            VentaMovimientoCuentaCorriente Movimiento,
            string MonedaCodigo,
            string ObraId,
            string? ObraNombre,
            string? NumeroComprobante);
    }

}

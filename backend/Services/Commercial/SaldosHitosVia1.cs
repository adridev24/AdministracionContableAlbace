using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Collections;
using Microsoft.EntityFrameworkCore;
namespace BudgetControl.Api.Services.Commercial;

public record SaldoHitoVia1(decimal Facturado = 0, decimal Reservado = 0, decimal Cobrado = 0, decimal FacturaActual = 0, bool TieneFacturas = false);
public static class SaldosHitosVia1
{
    private static decimal Round(decimal x) => decimal.Round(x, 2, MidpointRounding.AwayFromZero);
    public static async Task<Dictionary<int, SaldoHitoVia1>> ObtenerAsync(AppDbContext db, IEnumerable<int> hitoIds, int ventaActual = 0, DateTime? hasta = null)
    {
        var ids = hitoIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => new SaldoHitoVia1());
        if (ids.Count == 0) return result;
        var ventas = await db.VinculacionesFacturasHitos.Where(x => ids.Contains(x.HitoId)).Select(x => x.VentaId).Distinct().ToListAsync();
        if (ventas.Count == 0) return result;
        // Load every hito of each invoice: partial collections follow the stored allocation order.
        var links = await db.VinculacionesFacturasHitos.AsNoTracking().Include(x => x.Venta)
            .Where(x => ventas.Contains(x.VentaId)).ToListAsync();
        var pagos = await db.CobranzasAplicacionesFactura.AsNoTracking()
            .Where(a => ventas.Contains(a.VentaId) && a.Cobranza.Estado == CobranzaEstado.Confirmada && (!hasta.HasValue || a.Cobranza.Fecha <= hasta))
            .GroupBy(a => a.VentaId).Select(g => new { Id = g.Key, Total = g.Sum(a => a.ImporteAplicado) }).ToDictionaryAsync(a => a.Id, a => a.Total);
        var rechazos = await db.CobranzasMediosPagoAplicacionesFactura.AsNoTracking()
            .Where(a => ventas.Contains(a.AplicacionFactura.VentaId) && a.AplicacionFactura.Cobranza.Estado == CobranzaEstado.Confirmada && (!hasta.HasValue || a.AplicacionFactura.Cobranza.Fecha <= hasta) &&
                a.CobranzaMedioPago.ChequeTercero != null && a.CobranzaMedioPago.ChequeTercero.Estado == ChequeTerceroEstado.RECHAZADO)
            .GroupBy(a => a.AplicacionFactura.VentaId).Select(g => new { Id = g.Key, Total = g.Sum(a => a.ImporteAplicado) }).ToDictionaryAsync(a => a.Id, a => a.Total);
        foreach (var group in links.GroupBy(x => x.VentaId))
        {
            var venta = group.First().Venta;
            var cobrado = Math.Max(0, pagos.GetValueOrDefault(venta.Id) - rechazos.GetValueOrDefault(venta.Id));
            var restante = venta.Estado == VentaEstado.Confirmada && venta.Total > 0
                ? Round(group.Sum(x => x.Importe) * Math.Min(cobrado / venta.Total, 1)) : 0;
            foreach (var link in group.OrderBy(x => x.Orden).ThenBy(x => x.Id))
            {
                var aplicado = Math.Min(restante, link.Importe);
                restante -= aplicado;
                if (!result.TryGetValue(link.HitoId, out var saldo)) continue;
                result[link.HitoId] = saldo with {
                    Facturado = saldo.Facturado + (venta.Estado == VentaEstado.Confirmada && venta.Id != ventaActual ? link.Importe : 0),
                    Reservado = saldo.Reservado + (venta.Estado == VentaEstado.Borrador && venta.Id != ventaActual ? link.Importe : 0),
                    FacturaActual = saldo.FacturaActual + (venta.Id == ventaActual ? link.Importe : 0),
                    Cobrado = saldo.Cobrado + aplicado, TieneFacturas = true
                };
            }
        }
        return result;
    }
}

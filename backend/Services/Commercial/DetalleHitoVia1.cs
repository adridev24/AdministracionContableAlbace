using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Commercial;

public record CobranzaHitoDetalle(int CobranzaId, DateTime Fecha, string Estado, decimal AplicadoFactura, decimal Rechazado, decimal NetoHito);
public record FacturaHitoDetalle(int VentaId, string Comprobante, DateTime Fecha, string Estado, string MonedaFactura,
    decimal TotalFactura, decimal NetoHito, decimal CobradoHito, List<CobranzaHitoDetalle> Cobranzas);

public static class DetalleHitoVia1
{
    public static async Task<List<FacturaHitoDetalle>> ObtenerAsync(AppDbContext db, int acuerdoId, int viaId, int hitoId)
    {
        if (!await db.HitosComercialesVias.AnyAsync(h => h.Id == hitoId && h.AcuerdoComercialViaId == viaId &&
            h.AcuerdoComercialVia.AcuerdoComercialId == acuerdoId && h.AcuerdoComercialVia.ViaOperacion == ViaOperacion.Via1 &&
            h.AcuerdoComercialVia.ModalidadCobro == ModalidadCobro.Abierta))
            throw new InvalidOperationException("El hito no pertenece a una vía abierta de Vía 1 de este acuerdo.");
        var ventas = await db.VinculacionesFacturasHitos.Where(x => x.HitoId == hitoId).Select(x => x.VentaId).ToListAsync();
        var links = await db.VinculacionesFacturasHitos.AsNoTracking().Include(x => x.Venta)
            .Where(x => ventas.Contains(x.VentaId)).ToListAsync();
        var aplicaciones = await db.CobranzasAplicacionesFactura.AsNoTracking().Include(x => x.Cobranza)
            .Include(x => x.AplicacionesMediosPago).ThenInclude(x => x.CobranzaMedioPago).ThenInclude(x => x.ChequeTercero)
            .Where(x => ventas.Contains(x.VentaId)).ToListAsync();
        var result = new List<FacturaHitoDetalle>();
        foreach (var group in links.GroupBy(x => x.VentaId))
        {
            var venta = group.First().Venta;
            var orden = group.OrderBy(x => x.Orden).ThenBy(x => x.Id).ToList();
            var link = orden.Single(x => x.HitoId == hitoId);
            var previo = orden.TakeWhile(x => x.Id != link.Id).Sum(x => x.Importe);
            decimal bruto = 0, cobrado = 0;
            decimal Neto(decimal acumulado) => venta.Estado == VentaEstado.Confirmada && venta.Total > 0
                ? Math.Clamp(decimal.Round(group.Sum(x => x.Importe) * Math.Min(acumulado / venta.Total, 1), 2, MidpointRounding.AwayFromZero) - previo, 0, link.Importe) : 0;
            var cobranzas = new List<CobranzaHitoDetalle>();
            foreach (var app in aplicaciones.Where(x => x.VentaId == venta.Id).OrderBy(x => x.Cobranza.Fecha).ThenBy(x => x.CobranzaId).ThenBy(x => x.Id))
            {
                var rechazado = app.AplicacionesMediosPago.Where(x => x.CobranzaMedioPago.ChequeTercero?.Estado == ChequeTerceroEstado.RECHAZADO).Sum(x => x.ImporteAplicado);
                if (app.Cobranza.Estado == CobranzaEstado.Confirmada) bruto += Math.Max(0, app.ImporteAplicado - rechazado);
                var acumulado = Neto(bruto);
                cobranzas.Add(new(app.CobranzaId, app.Cobranza.Fecha, app.Cobranza.Estado.ToString(), app.ImporteAplicado, rechazado, acumulado - cobrado));
                cobrado = acumulado;
            }
            result.Add(new(venta.Id, $"{venta.PuntoVenta:D4}-{venta.NumeroComprobante:D8}", venta.FechaComprobante,
                venta.Estado.ToString(), venta.MonedaCodigo, venta.Total, link.Importe, cobrado, cobranzas));
        }
        return result.OrderBy(x => x.Fecha).ThenBy(x => x.VentaId).ToList();
    }
}

using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public class CalculadorVentasService : ICalculadorVentasService
    {
        public VentaDetalleCalculo CalcularDetalle(
            decimal cantidad,
            decimal precioUnitario,
            decimal porcentajeDescuento,
            TipoComprobanteVenta comprobante,
            AlicuotaIvaVenta tratamientoIva)
        {
            var importeBruto = RoundMoney(cantidad * precioUnitario);
            var importeDescuento = RoundMoney(importeBruto * porcentajeDescuento / 100m);
            var neto = RoundMoney(importeBruto - importeDescuento);
            var porcentajeIva = comprobante.PermiteIva && !comprobante.EsExportacion && tratamientoIva.TipoTratamiento == TipoTratamientoIvaVenta.Gravado
                ? tratamientoIva.Porcentaje
                : 0m;
            var importeIva = RoundMoney(neto * porcentajeIva / 100m);
            var totalLinea = RoundMoney(neto + importeIva);

            return new VentaDetalleCalculo(importeBruto, importeDescuento, neto, porcentajeIva, importeIva, totalLinea);
        }

        public void RecalcularTotales(Venta venta)
        {
            var detalles = venta.Detalles ?? new List<VentaDetalle>();
            var percepciones = venta.PercepcionesIibb ?? new List<VentaPercepcionIibb>();

            venta.SubtotalBruto = RoundMoney(detalles.Sum(d => d.ImporteBruto));
            venta.TotalDescuentos = RoundMoney(detalles.Sum(d => d.ImporteDescuento));
            venta.NetoGravado = RoundMoney(detalles.Where(d => d.TipoTratamientoIva == TipoTratamientoIvaVenta.Gravado).Sum(d => d.Neto));
            venta.TotalExento = RoundMoney(detalles.Where(d => d.TipoTratamientoIva == TipoTratamientoIvaVenta.Exento).Sum(d => d.Neto));
            venta.TotalNoGravado = RoundMoney(detalles.Where(d => d.TipoTratamientoIva == TipoTratamientoIvaVenta.NoGravado).Sum(d => d.Neto));
            venta.TotalIva = RoundMoney(detalles.Sum(d => d.ImporteIva));
            venta.TotalAntesPercepciones = RoundMoney(detalles.Sum(d => d.TotalLinea));
            venta.TotalPercepciones = RoundMoney(percepciones.Where(p => p.Activa).Sum(p => p.Importe));
            venta.Total = RoundMoney(venta.TotalAntesPercepciones + venta.TotalPercepciones);
        }

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}

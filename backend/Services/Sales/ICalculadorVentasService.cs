using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface ICalculadorVentasService
    {
        VentaDetalleCalculo CalcularDetalle(
            decimal cantidad,
            decimal precioUnitario,
            decimal porcentajeDescuento,
            TipoComprobanteVenta comprobante,
            AlicuotaIvaVenta tratamientoIva);

        void RecalcularTotales(Venta venta);
    }

    public sealed record VentaDetalleCalculo(
        decimal ImporteBruto,
        decimal ImporteDescuento,
        decimal Neto,
        decimal PorcentajeIvaAplicado,
        decimal ImporteIva,
        decimal TotalLinea);
}

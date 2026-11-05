using System.Text.Json;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.Services.Sales;

public static class ImputacionPlanFactura
{
    public static FiscalRegistrado? Fiscal(Venta venta) => venta.FiscalSimpleJson == null
        ? null : JsonSerializer.Deserialize<FiscalRegistrado>(venta.FiscalSimpleJson);

    public static decimal Neto(Venta venta) => Fiscal(venta)?.ImputaNetoPlan == true
        ? venta.NetoGravado : venta.TotalAntesPercepciones - venta.TotalIva;

    public static decimal Cotizacion(Venta venta, string monedaPlan)
    {
        if (monedaPlan == venta.MonedaCodigo) return 1;
        if (venta.MonedaCodigo == "ARS" && monedaPlan == "USD" && Fiscal(venta)?.CotizacionDolarReferencia is decimal tasa && tasa > 0)
            return tasa;
        throw new InvalidOperationException("Para imputar la factura en pesos a cuotas en dólares, indique la cotización de referencia de la factura.");
    }

    public static decimal Importe(Venta venta, string monedaPlan) =>
        decimal.Round(Neto(venta) / Cotizacion(venta, monedaPlan), 2, MidpointRounding.AwayFromZero);
}

using BudgetControl.Api.Models.Fiscal;

namespace BudgetControl.Api.DTOs.Fiscal;

public record ObservacionFiscal(string Nivel, string Origen, int? Id, string Mensaje);
public record AlicuotaFiscal(decimal Porcentaje, decimal Neto, decimal Iva);
public class VentaLibroFiscal
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string ClienteId { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int PuntoVenta { get; set; }
    public long Numero { get; set; }
    public int Signo { get; set; }
    public string Moneda { get; set; } = "";
    public decimal Cotizacion { get; set; }
    public decimal Neto { get; set; }
    public decimal Iva { get; set; }
    public decimal Exento { get; set; }
    public decimal NoGravado { get; set; }
    public decimal Percepciones { get; set; }
    public decimal Total { get; set; }
    public decimal DevolucionCheques { get; set; }
    public List<AlicuotaFiscal> Alicuotas { get; set; } = [];
    public VentaFiscal Fiscal { get; set; } = new();
    public bool DatosConfirmados { get; set; }
    public string Clave => $"{Fiscal.TipoComprobanteArca:000}-{PuntoVenta:00000}-{Numero:00000000000000000000}";
}
public record RetencionReporte(int Id, int CobranzaId, string ClienteId, DateTime FechaCobranza,
    string Impuesto, string Moneda, decimal Cotizacion, decimal Importe, RetencionFiscal? Fiscal);
public class ReporteFiscal
{
    public string Periodo { get; set; } = "";
    public EmpresaFiscal? Empresa { get; set; }
    public List<VentaLibroFiscal> Ventas { get; set; } = [];
    public List<RetencionReporte> Retenciones { get; set; } = [];
    public List<ObservacionFiscal> Observaciones { get; set; } = [];
    public bool PuedeExportar => Ventas.Count > 0 && !Observaciones.Any(x => x.Nivel == "Error" && x.Origen != "Retencion");
    public decimal NetoPesos => Ventas.Sum(x => decimal.Round(x.Signo * x.Neto * x.Cotizacion, 2, MidpointRounding.AwayFromZero));
    public decimal IvaPesos => Ventas.Sum(x => decimal.Round(x.Signo * x.Iva * x.Cotizacion, 2, MidpointRounding.AwayFromZero));
    public Dictionary<string, decimal> RetencionesPorImpuesto => Retenciones.Where(x => x.Fiscal?.PeriodoComputo == Periodo)
        .GroupBy(x => x.Impuesto).ToDictionary(g => g.Key, g => g.Sum(x => decimal.Round(x.Importe * x.Cotizacion, 2, MidpointRounding.AwayFromZero)));
}
public record DiferenciaFiscal(string Clave, string Estado, string Detalle);
public record ConciliacionFiscal(int RegistrosSistema, int RegistrosReferencia, int Coincidentes,
    List<DiferenciaFiscal> Diferencias);

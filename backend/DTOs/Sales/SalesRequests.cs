using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.DTOs.Sales
{
    public class VentaHeaderRequest
    {
        [Required]
        public int TipoComprobanteVentaId { get; set; }

        public int? PuntoVentaComprobanteId { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public DateTime FechaComprobante { get; set; }

        [Range(1, int.MaxValue)]
        public int PuntoVenta { get; set; }

        [Range(1, long.MaxValue)]
        public long NumeroComprobante { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        [Range(0.000001, double.MaxValue)]
        public decimal Cotizacion { get; set; } = 1;

        public string? Observaciones { get; set; }
    }

    public class TipoComprobanteVentaRequest
    {
        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public string? Letra { get; set; }
        public string TipoFiscal { get; set; } = "Local";
        public bool EsCreditoElectronica { get; set; }
        public bool EsExportacion { get; set; }
        public bool RequiereNomenclador { get; set; }
        public bool PermiteIva { get; set; } = true;
        public int Signo { get; set; } = 1;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
    }

    public class PuntoVentaRequest
    {
        [Range(1, int.MaxValue)]
        public int Numero { get; set; }

        [Required]
        public string Descripcion { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public string? Observaciones { get; set; }
        public List<int>? ComprobantesPermitidosIds { get; set; }
    }

    public class PuntoVentaComprobanteRequest
    {
        [Required]
        public int TipoComprobanteVentaId { get; set; }

        public bool Activo { get; set; } = true;
        public string? Descripcion { get; set; }
    }

    public class VentaListFilterRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? ClienteExternoId { get; set; }
        public string? ObraExternaId { get; set; }
        public int? TipoComprobanteVentaId { get; set; }
        public int? PuntoVentaComprobanteId { get; set; }
        public int? PuntoVenta { get; set; }
        public long? NumeroComprobante { get; set; }
        public VentaEstado? Estado { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AlicuotaIvaVentaRequest
    {
        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        public TipoTratamientoIvaVenta TipoTratamiento { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Porcentaje { get; set; }

        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
    }

    public class NomencladorFceRequest
    {
        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
    }

    public class PercepcionIibbEntreRiosRequest
    {
        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public string Jurisdiccion { get; set; } = "Entre Rios";
        public string TipoTributo { get; set; } = "PERCEPCION_IIBB";

        [Required]
        public string NumeroRegimen { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal Porcentaje { get; set; }

        [Required]
        public TipoBaseCalculoPercepcionIibb TipoBaseCalculo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MontoMinimo { get; set; }

        [Required]
        public DateTime VigenciaDesde { get; set; }

        public DateTime? VigenciaHasta { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
    }
}

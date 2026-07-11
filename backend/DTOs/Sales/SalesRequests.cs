using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.DTOs.Sales
{
    public class VentaHeaderRequest
    {
        [Required]
        public int TipoComprobanteVentaId { get; set; }

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

    public class VentaListFilterRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? ClienteExternoId { get; set; }
        public string? ObraExternaId { get; set; }
        public int? TipoComprobanteVentaId { get; set; }
        public int? PuntoVenta { get; set; }
        public long? NumeroComprobante { get; set; }
        public VentaEstado? Estado { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}

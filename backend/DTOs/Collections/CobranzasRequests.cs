using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Collections;

namespace BudgetControl.Api.DTOs.Collections
{
    public class CobranzaListFilterRequest
    {
        public string? ClienteExternoId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? MonedaCodigo { get; set; }
        public CobranzaEstado? Estado { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class CobranzaHeaderRequest
    {
        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        [Range(0.000001, double.MaxValue)]
        public decimal Cotizacion { get; set; } = 1;

        [Range(0.01, double.MaxValue)]
        public decimal ImporteTotal { get; set; }

        public string? Observaciones { get; set; }
    }

    public class CobranzaMedioPagoRequest
    {
        [Required]
        public int MedioPagoCobranzaId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Importe { get; set; }

        public int? BancoCobranzaId { get; set; }
        public string? Banco { get; set; }
        public string? NumeroReferencia { get; set; }
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaValor { get; set; }
        public string? Librador { get; set; }
        public string? CuitLibrador { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CobranzaAplicacionFacturaRequest
    {
        [Required]
        public int VentaId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal ImporteAplicado { get; set; }
    }

    public class AnularCobranzaRequest
    {
        [Required]
        public string Motivo { get; set; } = null!;
    }
}

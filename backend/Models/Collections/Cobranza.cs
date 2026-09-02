using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class Cobranza
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        public DateTime Fecha { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        public decimal Cotizacion { get; set; } = 1;
        public decimal ImporteTotal { get; set; }
        public CobranzaEstado Estado { get; set; } = CobranzaEstado.Borrador;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public string? UsuarioConfirmacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
        public string? UsuarioAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }
        public int? AsientoContableId { get; set; }

        public ICollection<CobranzaMedioPago> MediosPago { get; set; } = new List<CobranzaMedioPago>();
        public ICollection<CobranzaAplicacionFactura> AplicacionesFactura { get; set; } = new List<CobranzaAplicacionFactura>();
    }
}

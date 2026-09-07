using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class CobranzaMedioPagoAplicacionFactura
    {
        public int Id { get; set; }
        public int CobranzaMedioPagoId { get; set; }
        public int CobranzaAplicacionFacturaId { get; set; }
        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public CobranzaMedioPago CobranzaMedioPago { get; set; } = null!;
        public CobranzaAplicacionFactura AplicacionFactura { get; set; } = null!;
    }
}

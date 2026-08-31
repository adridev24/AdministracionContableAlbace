using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.Models.Collections
{
    public class CobranzaAplicacionFactura
    {
        public int Id { get; set; }
        public int CobranzaId { get; set; }
        public int VentaId { get; set; }
        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public Cobranza Cobranza { get; set; } = null!;
        public Venta Venta { get; set; } = null!;
        public ICollection<CobranzaAplicacionObligacion> AplicacionesObligacion { get; set; } = new List<CobranzaAplicacionObligacion>();
    }
}

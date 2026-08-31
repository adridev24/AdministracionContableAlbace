using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.Models.Collections
{
    public class CobranzaAplicacionObligacion
    {
        public int Id { get; set; }
        public int CobranzaAplicacionFacturaId { get; set; }
        public int CuotaComercialId { get; set; }

        [Required]
        public string TipoObligacion { get; set; } = null!;

        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public CobranzaAplicacionFactura AplicacionFactura { get; set; } = null!;
        public CuotaComercial CuotaComercial { get; set; } = null!;
    }
}

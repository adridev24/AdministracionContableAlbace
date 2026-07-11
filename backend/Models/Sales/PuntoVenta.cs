using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class PuntoVenta
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int Numero { get; set; }

        [Required]
        public string Descripcion { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public ICollection<PuntoVentaComprobante> Comprobantes { get; set; } = new List<PuntoVentaComprobante>();
    }
}

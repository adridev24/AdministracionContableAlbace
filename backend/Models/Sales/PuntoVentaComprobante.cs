using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class PuntoVentaComprobante
    {
        public int Id { get; set; }
        public int PuntoVentaId { get; set; }
        public int TipoComprobanteVentaId { get; set; }
        public bool Activo { get; set; } = true;
        public string? Descripcion { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public PuntoVenta PuntoVenta { get; set; } = null!;
        public TipoComprobanteVenta TipoComprobante { get; set; } = null!;
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}

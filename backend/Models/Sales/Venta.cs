using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class Venta
    {
        public int Id { get; set; }

        public int TipoComprobanteVentaId { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        public DateTime FechaComprobante { get; set; }
        public int PuntoVenta { get; set; }
        public long NumeroComprobante { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        public decimal Cotizacion { get; set; } = 1;
        public VentaEstado Estado { get; set; } = VentaEstado.Borrador;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public TipoComprobanteVenta TipoComprobante { get; set; } = null!;
    }
}

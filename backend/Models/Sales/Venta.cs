using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class Venta
    {
        public int Id { get; set; }

        public int TipoComprobanteVentaId { get; set; }
        public int? PuntoVentaComprobanteId { get; set; }

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
        public decimal SubtotalBruto { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoGravado { get; set; }
        public decimal TotalExento { get; set; }
        public decimal TotalNoGravado { get; set; }
        public decimal TotalIva { get; set; }
        public decimal TotalAntesPercepciones { get; set; }
        public decimal TotalPercepciones { get; set; }
        public decimal Total { get; set; }
        public bool PercepcionIibbRequiereRecalculo { get; set; }
        public DateTime? FechaUltimoCalculoPercepcion { get; set; }
        public VentaEstado Estado { get; set; } = VentaEstado.Borrador;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public string? UsuarioConfirmacion { get; set; }
        public int? AsientoContableId { get; set; }

        public TipoComprobanteVenta TipoComprobante { get; set; } = null!;
        public PuntoVentaComprobante? PuntoVentaComprobante { get; set; }
        public ICollection<VentaDetalle> Detalles { get; set; } = new List<VentaDetalle>();
        public ICollection<VentaPercepcionIibb> PercepcionesIibb { get; set; } = new List<VentaPercepcionIibb>();
    }
}

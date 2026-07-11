using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class TipoComprobanteVenta
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public string? Letra { get; set; }
        public string TipoFiscal { get; set; } = "Local";
        public bool EsCreditoElectronica { get; set; }
        public bool EsExportacion { get; set; }
        public bool RequiereNomenclador { get; set; }
        public bool PermiteIva { get; set; } = true;
        public int Signo { get; set; } = 1;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = "Sistema";
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<PuntoVentaComprobante> PuntosVentaComprobantes { get; set; } = new List<PuntoVentaComprobante>();
    }
}

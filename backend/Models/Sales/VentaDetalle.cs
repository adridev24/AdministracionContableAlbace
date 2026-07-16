using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class VentaDetalle
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int NumeroLinea { get; set; }
        public int? ItemFacturableId { get; set; }
        public string? CodigoItem { get; set; }
        public string? ItemFacturableDescripcion { get; set; }
        public int? CategoriaItemFacturableId { get; set; }
        public string? CategoriaItemFacturableCodigo { get; set; }
        public string? CategoriaItemFacturableDescripcion { get; set; }
        public int? UnidadMedidaVentaId { get; set; }
        public string? UnidadMedidaCodigo { get; set; }
        public string? UnidadMedidaDescripcion { get; set; }
        public string? UnidadMedidaAbreviatura { get; set; }

        [Required]
        public string Descripcion { get; set; } = null!;

        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal ImporteBruto { get; set; }
        public decimal ImporteDescuento { get; set; }
        public decimal Neto { get; set; }
        public int TratamientoIvaId { get; set; }
        public string TratamientoIvaCodigo { get; set; } = null!;
        public string TratamientoIvaDescripcion { get; set; } = null!;
        public TipoTratamientoIvaVenta TipoTratamientoIva { get; set; }
        public decimal PorcentajeIvaAplicado { get; set; }
        public decimal ImporteIva { get; set; }
        public int? NomencladorId { get; set; }
        public string? NomencladorCodigo { get; set; }
        public string? NomencladorDescripcion { get; set; }
        public decimal TotalLinea { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public Venta Venta { get; set; } = null!;
        public ItemFacturable? ItemFacturable { get; set; }
        public CategoriaItemFacturable? CategoriaItemFacturable { get; set; }
        public UnidadMedidaVenta? UnidadMedida { get; set; }
        public AlicuotaIvaVenta TratamientoIva { get; set; } = null!;
        public NomencladorFce? Nomenclador { get; set; }
    }
}

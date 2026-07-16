using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class ItemFacturable
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public string? DescripcionAmpliada { get; set; }
        public int? CategoriaItemFacturableId { get; set; }
        public int UnidadMedidaVentaId { get; set; }
        public int TratamientoIvaPredeterminadoId { get; set; }
        public int? NomencladorPredeterminadoId { get; set; }
        public decimal? PrecioPredeterminado { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public CategoriaItemFacturable? Categoria { get; set; }
        public UnidadMedidaVenta UnidadMedida { get; set; } = null!;
        public AlicuotaIvaVenta TratamientoIvaPredeterminado { get; set; } = null!;
        public NomencladorFce? NomencladorPredeterminado { get; set; }
    }
}

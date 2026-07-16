using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class UnidadMedidaVenta
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        public string? Abreviatura { get; set; }
        public bool PermiteDecimales { get; set; } = true;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public ICollection<ItemFacturable> Items { get; set; } = new List<ItemFacturable>();
    }
}

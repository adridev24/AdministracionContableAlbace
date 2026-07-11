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
        public int Signo { get; set; } = 1;
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }

        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class VentaMovimientoCuentaCorriente
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        public DateTime Fecha { get; set; }

        [Required]
        public string TipoMovimiento { get; set; } = null!;

        public decimal Debe { get; set; }
        public decimal Haber { get; set; }

        [Required]
        public string ModuloOrigen { get; set; } = null!;

        [Required]
        public string IdOrigen { get; set; } = null!;

        public string? Descripcion { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;
    }
}

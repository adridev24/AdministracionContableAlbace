using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class BancoCobranza
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public ICollection<CobranzaMedioPago> CobranzasMediosPago { get; set; } = new List<CobranzaMedioPago>();
    }
}

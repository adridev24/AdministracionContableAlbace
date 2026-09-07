using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Treasury;

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

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public ICollection<CobranzaMedioPago> CobranzasMediosPago { get; set; } = new List<CobranzaMedioPago>();
        public ICollection<CuentaBancariaEmpresa> CuentasBancariasEmpresa { get; set; } = new List<CuentaBancariaEmpresa>();
    }
}

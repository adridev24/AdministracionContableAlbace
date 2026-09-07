using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Accounting;
using BudgetControl.Api.Models.Collections;

namespace BudgetControl.Api.Models.Treasury
{
    public class CuentaBancariaEmpresa
    {
        public int Id { get; set; }
        public int BancoId { get; set; }

        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        public string TipoCuenta { get; set; } = null!;

        [Required]
        public string MonedaCodigo { get; set; } = null!;

        [Required]
        public string NumeroCuenta { get; set; } = null!;

        public string? Cbu { get; set; }
        public string? AliasCbu { get; set; }
        public int CuentaContableId { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public BancoCobranza Banco { get; set; } = null!;
        public CuentaContable CuentaContable { get; set; } = null!;
        public ICollection<ChequeTercero> ChequesDepositados { get; set; } = new List<ChequeTercero>();
    }
}

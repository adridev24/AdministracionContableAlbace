using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class ChequeTercero
    {
        public int Id { get; set; }
        public int CobranzaMedioPagoId { get; set; }
        public int BancoCobranzaId { get; set; }

        [Required]
        public string NumeroCheque { get; set; } = null!;

        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Importe { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = null!;

        [Required]
        public string Librador { get; set; } = null!;

        [Required]
        public string CuitLibrador { get; set; } = null!;

        public ChequeTerceroEstado Estado { get; set; } = ChequeTerceroEstado.EN_CARTERA;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaDeposito { get; set; }
        public string? BancoDestino { get; set; }
        public string? CuentaDestino { get; set; }
        public string? UsuarioDeposito { get; set; }
        public DateTime? FechaAcreditacion { get; set; }
        public string? UsuarioAcreditacion { get; set; }

        public CobranzaMedioPago CobranzaMedioPago { get; set; } = null!;
        public BancoCobranza BancoCatalogo { get; set; } = null!;
    }
}

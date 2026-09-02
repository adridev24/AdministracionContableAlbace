using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Collections;

namespace BudgetControl.Api.DTOs.Collections
{
    public class CarteraChequesFilterRequest
    {
        public ChequeTerceroEstado? Estado { get; set; }
        public DateTime? FechaVencimientoDesde { get; set; }
        public DateTime? FechaVencimientoHasta { get; set; }
        public string? Moneda { get; set; }
        public int? BancoId { get; set; }
        public string? ClienteId { get; set; }
    }

    public class ChequeTerceroListResponse
    {
        public int Id { get; set; }
        public string NumeroCheque { get; set; } = string.Empty;
        public int BancoCobranzaId { get; set; }
        public string Banco { get; set; } = string.Empty;
        public DateTime FechaVencimiento { get; set; }
        public decimal Importe { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public string Librador { get; set; } = string.Empty;
        public ChequeTerceroEstado Estado { get; set; }
        public string ClienteExternoId { get; set; } = string.Empty;
        public int CobranzaId { get; set; }
    }

    public class ChequeTerceroDetalleResponse : ChequeTerceroListResponse
    {
        public int CobranzaMedioPagoId { get; set; }
        public DateTime FechaEmision { get; set; }
        public string CuitLibrador { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaDeposito { get; set; }
        public string? BancoDestino { get; set; }
        public string? CuentaDestino { get; set; }
        public string? UsuarioDeposito { get; set; }
        public DateTime? FechaAcreditacion { get; set; }
        public string? UsuarioAcreditacion { get; set; }
        public string MedioPagoCodigo { get; set; } = string.Empty;
        public string MedioPagoDescripcion { get; set; } = string.Empty;
    }

    public class DepositarChequeTerceroRequest
    {
        [Required]
        public DateTime FechaDeposito { get; set; }

        [Required]
        public string BancoDestino { get; set; } = null!;

        [Required]
        public string CuentaDestino { get; set; } = null!;
    }

    public class AcreditarChequeTerceroRequest
    {
        [Required]
        public DateTime FechaAcreditacion { get; set; }
    }
}

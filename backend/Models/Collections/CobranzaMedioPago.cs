using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class CobranzaMedioPago
    {
        public int Id { get; set; }
        public int CobranzaId { get; set; }
        public int MedioPagoCobranzaId { get; set; }
        public int? BancoCobranzaId { get; set; }
        public decimal Importe { get; set; }
        public string? Banco { get; set; }
        public string? NumeroReferencia { get; set; }
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaValor { get; set; }
        public string? Librador { get; set; }
        public string? CuitLibrador { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public Cobranza Cobranza { get; set; } = null!;
        public MedioPagoCobranza MedioPago { get; set; } = null!;
        public BancoCobranza? BancoCatalogo { get; set; }
        public ChequeTercero? ChequeTercero { get; set; }
    }
}

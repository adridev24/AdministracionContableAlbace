using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class VentaPercepcionIibb
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int? RegimenPercepcionIibbId { get; set; }
        public string? CodigoRegimenAplicado { get; set; }
        public string? DescripcionRegimenAplicada { get; set; }
        public string? JurisdiccionAplicada { get; set; }
        public string? TipoTributoAplicado { get; set; }
        public string? NumeroRegimenAplicado { get; set; }
        public TipoBaseCalculoPercepcionIibb? TipoBaseCalculo { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal AlicuotaAplicada { get; set; }
        public decimal Importe { get; set; }
        public DateTime? VigenciaDesdeAplicada { get; set; }
        public DateTime? VigenciaHastaAplicada { get; set; }
        public ResultadoPercepcionIibb Resultado { get; set; }
        public string? Motivo { get; set; }
        public bool Activa { get; set; } = true;
        public bool EsAutomatica { get; set; } = true;
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public Venta Venta { get; set; } = null!;
        public PercepcionIibbEntreRios? RegimenPercepcionIibb { get; set; }
    }
}

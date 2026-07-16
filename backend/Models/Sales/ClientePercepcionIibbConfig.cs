using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class ClientePercepcionIibbConfig
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        public SituacionPercepcionIibbCliente Situacion { get; set; } = SituacionPercepcionIibbCliente.Pendiente;
        public int? RegimenPercepcionIibbId { get; set; }
        public string? NumeroInscripcionIibb { get; set; }
        public string? JurisdiccionIibb { get; set; }
        public DateTime? ExclusionDesde { get; set; }
        public DateTime? ExclusionHasta { get; set; }
        public string? MotivoExclusion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }

        public PercepcionIibbEntreRios? RegimenPercepcionIibb { get; set; }
    }
}

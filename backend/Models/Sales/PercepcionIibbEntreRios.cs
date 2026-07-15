using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Sales
{
    public class PercepcionIibbEntreRios
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        public string Jurisdiccion { get; set; } = "Entre Rios";

        [Required]
        public string TipoTributo { get; set; } = "PERCEPCION_IIBB";

        [Required]
        public string NumeroRegimen { get; set; } = null!;

        public decimal Porcentaje { get; set; }
        public TipoBaseCalculoPercepcionIibb TipoBaseCalculo { get; set; }
        public decimal? MontoMinimo { get; set; }
        public DateTime VigenciaDesde { get; set; }
        public DateTime? VigenciaHasta { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}

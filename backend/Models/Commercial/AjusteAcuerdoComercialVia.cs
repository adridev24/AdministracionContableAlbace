using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class AjusteAcuerdoComercialVia
    {
        public int Id { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public ViaOperacion ViaOperacion { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = null!;

        public decimal MontoAnterior { get; set; }
        public decimal MontoNuevo { get; set; }
        public decimal Diferencia { get; set; }
        public TipoAjusteVia TipoAjuste { get; set; }

        [Required]
        public string Motivo { get; set; } = null!;

        public DateTime FechaAjuste { get; set; }

        [Required]
        public string UsuarioAjuste { get; set; } = null!;

        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
    }
}

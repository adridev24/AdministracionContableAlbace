using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class AjusteCuotaComercial
    {
        public int Id { get; set; }
        public int CuotaComercialId { get; set; }
        public int PlanPagoId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public TipoAjuste TipoAjuste { get; set; }
        public decimal? ImporteAnterior { get; set; }
        public decimal? ImporteNuevo { get; set; }
        public DateTime? FechaVencimientoAnterior { get; set; }
        public DateTime? FechaVencimientoNueva { get; set; }
        [Required]
        public string Motivo { get; set; } = null!;
        public DateTime FechaAjuste { get; set; }
        [Required]
        public string UsuarioAjuste { get; set; } = null!;

        public CuotaComercial CuotaComercial { get; set; } = null!;
        public PlanPago PlanPago { get; set; } = null!;
        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
    }
}

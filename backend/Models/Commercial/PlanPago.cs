using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class PlanPago
    {
        public int Id { get; set; }
        public int? AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public bool TieneAnticipo { get; set; }
        public decimal MontoAnticipo { get; set; }
        public int CantidadCuotas { get; set; }
        public DateTime FechaPrimerVencimiento { get; set; }
        public string Periodicidad { get; set; } = null!;
        public string? Observaciones { get; set; }

        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public ICollection<CuotaComercial> Cuotas { get; set; } = new List<CuotaComercial>();
    }
}

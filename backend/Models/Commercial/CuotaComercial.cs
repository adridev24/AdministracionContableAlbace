using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class CuotaComercial
    {
        public int Id { get; set; }
        public int PlanPagoId { get; set; }
        public int NumeroCuota { get; set; }
        public TipoCuota TipoCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal ImporteOriginal { get; set; }
        public decimal ImportePagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public CuotaEstado Estado { get; set; }

        public PlanPago PlanPago { get; set; } = null!;
        public ICollection<AplicacionPagoComercial> Aplicaciones { get; set; } = new List<AplicacionPagoComercial>();
        public ICollection<VinculacionFacturaComercial> VinculacionesFactura { get; set; } = new List<VinculacionFacturaComercial>();
        public ICollection<AjusteCuotaComercial> Ajustes { get; set; } = new List<AjusteCuotaComercial>();
    }
}

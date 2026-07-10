using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class AcuerdoComercialVia
    {
        public int Id { get; set; }
        public int AcuerdoComercialId { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public ModalidadCobro ModalidadCobro { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        public decimal MontoOriginal { get; set; }
        public decimal MontoActual { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
        public PlanPago? PlanPago { get; set; }
        public ICollection<PagoComercial> Pagos { get; set; } = new List<PagoComercial>();
        public ICollection<HitoComercialVia> Hitos { get; set; } = new List<HitoComercialVia>();
        public ICollection<AjusteAcuerdoComercialVia> Ajustes { get; set; } = new List<AjusteAcuerdoComercialVia>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class PagoComercial
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        public int AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public DateTime FechaPago { get; set; }
        public string MonedaCodigo { get; set; } = "ARS";
        public decimal ImporteTotal { get; set; }
        public string MedioPago { get; set; } = null!;
        public TipoImputacion TipoImputacion { get; set; }
        public OrigenPago OrigenPago { get; set; }
        public string? Observaciones { get; set; }
        public PagoEstado Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;

        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public ICollection<AplicacionPagoComercial> Aplicaciones { get; set; } = new List<AplicacionPagoComercial>();
    }
}

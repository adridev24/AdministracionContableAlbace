using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class AcuerdoComercial
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public string NumeroAcuerdo { get; set; } = null!;

        public DateTime FechaAcuerdo { get; set; }
        public string? Descripcion { get; set; }
        public decimal MontoTotal { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;

        public ICollection<AcuerdoComercialVia> Vias { get; set; } = new List<AcuerdoComercialVia>();
        public ICollection<PagoComercial> Pagos { get; set; } = new List<PagoComercial>();
    }
}

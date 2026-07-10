namespace BudgetControl.Api.Models.Commercial
{
    public class HitoComercialVia
    {
        public int Id { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal ImporteEstimado { get; set; }
        public DateTime FechaReferencia { get; set; }
        public decimal ImporteAplicado { get; set; }
        public HitoEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;

        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public ICollection<AplicacionPagoComercial> Aplicaciones { get; set; } = new List<AplicacionPagoComercial>();
    }
}

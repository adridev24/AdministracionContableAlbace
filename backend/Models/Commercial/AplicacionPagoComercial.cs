namespace BudgetControl.Api.Models.Commercial
{
    public class AplicacionPagoComercial
    {
        public int Id { get; set; }
        public int PagoComercialId { get; set; }
        public int? CuotaComercialId { get; set; }
        public int? HitoComercialViaId { get; set; }
        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public TipoImputacion TipoImputacion { get; set; }
        public string? Observaciones { get; set; }
        public string UsuarioAplicacion { get; set; } = null!;

        public PagoComercial PagoComercial { get; set; } = null!;
        public CuotaComercial? CuotaComercial { get; set; }
        public HitoComercialVia? HitoComercialVia { get; set; }
    }
}

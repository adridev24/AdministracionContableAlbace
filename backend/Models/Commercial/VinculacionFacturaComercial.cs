namespace BudgetControl.Api.Models.Commercial
{
    public class VinculacionFacturaComercial
    {
        public int Id { get; set; }
        public int CuotaComercialId { get; set; }
        public string FacturaExternaId { get; set; } = null!;
        public string NumeroFactura { get; set; } = null!;
        public decimal ImporteVinculado { get; set; }
        public DateTime FechaVinculacion { get; set; }

        public CuotaComercial CuotaComercial { get; set; } = null!;
    }
}

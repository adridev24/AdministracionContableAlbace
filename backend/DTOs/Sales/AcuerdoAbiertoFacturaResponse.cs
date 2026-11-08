namespace BudgetControl.Api.DTOs.Sales
{
    public class AcuerdoAbiertoFacturaResponse
    {
        public int AcuerdoId { get; set; }
        public int ViaId { get; set; }
        public string NumeroAcuerdo { get; set; } = null!;
        public string MonedaCodigo { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public decimal SaldoFacturable { get; set; }
        public decimal MontoActual { get; set; }
        public List<HitoFacturaConsultaResponse> Hitos { get; set; } = new();
    }
    public class HitoFacturaConsultaResponse
    {
        public decimal Facturado { get; set; }
        public decimal Reservado { get; set; }
        public decimal Cobrado { get; set; }
        public decimal SaldoDisponible { get; set; }
        public decimal ImporteAplicadoFacturaActual { get; set; }
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public DateTime FechaReferencia { get; set; }
        public decimal ImporteEstimado { get; set; }
        public string Estado { get; set; } = null!;
    }
}

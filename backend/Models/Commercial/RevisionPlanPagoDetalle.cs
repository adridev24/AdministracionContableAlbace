namespace BudgetControl.Api.Models.Commercial
{
    public class RevisionPlanPagoDetalle
    {
        public int Id { get; set; }
        public int RevisionPlanPagoId { get; set; }
        public int PlanPagoId { get; set; }
        // Null for a physically deleted installment. The immutable snapshot survives.
        public int? CuotaComercialId { get; set; }
        public int CuotaOriginalId { get; set; }
        public int NumeroCuota { get; set; }
        public TipoCuota TipoCuota { get; set; }
        public OperacionRevisionPlanPago Operacion { get; set; }
        public decimal? ImporteAnterior { get; set; }
        public decimal? ImporteNuevo { get; set; }
        public DateTime? VencimientoAnterior { get; set; }
        public DateTime? VencimientoNuevo { get; set; }
        public CuotaEstado? EstadoAnterior { get; set; }
        public CuotaEstado? EstadoNuevo { get; set; }
        public decimal? ImportePagadoAnterior { get; set; }
        public decimal? SaldoPendienteAnterior { get; set; }
        public RevisionPlanPago RevisionPlanPago { get; set; } = null!;
        public CuotaComercial? CuotaComercial { get; set; }
    }
}

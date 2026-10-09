using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial
{
    // Read contracts only. User/date and monetary differences are never trusted request fields.
    public class RevisionPlanPagoResponse
    {
        public int Id { get; set; }
        public Guid SolicitudId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int PlanPagoId { get; set; }
        public decimal MontoAnterior { get; set; }
        public decimal MontoNuevo { get; set; }
        public decimal Diferencia { get; set; }
        public string Comentario { get; set; } = null!;
        public string UsuarioId { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public TipoRevisionPlanPago TipoRevision { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public List<RevisionPlanPagoDetalleResponse> Detalles { get; set; } = new();
    }

    public class RevisionPlanPagoDetalleResponse
    {
        public int Id { get; set; }
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
    }
}

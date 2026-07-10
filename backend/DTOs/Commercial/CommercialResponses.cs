using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial
{
    public class AcuerdoResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public string NumeroAcuerdo { get; set; } = null!;
        public DateTime FechaAcuerdo { get; set; }
        public string? Descripcion { get; set; }
        public decimal MontoTotal { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public ViaOperacion? ViaOperacion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public List<AcuerdoViaResponse> Vias { get; set; } = new();
    }

    public class AcuerdoDetalleResponse : AcuerdoResponse
    {
        public PlanPagoResponse? PlanPago { get; set; }
        public List<PagoComercialResponse> Pagos { get; set; } = new();
    }

    public class AcuerdoViaResponse
    {
        public int Id { get; set; }
        public int AcuerdoComercialId { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public ModalidadCobro ModalidadCobro { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public decimal MontoOriginal { get; set; }
        public decimal MontoActual { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public PlanPagoResponse? PlanPago { get; set; }
        public List<PagoComercialResponse> Pagos { get; set; } = new();
        public List<HitoComercialResponse> Hitos { get; set; } = new();
        public List<AjusteAcuerdoViaResponse> Ajustes { get; set; } = new();
    }

    public class PlanPagoResponse
    {
        public int Id { get; set; }
        public int? AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public bool TieneAnticipo { get; set; }
        public decimal MontoAnticipo { get; set; }
        public int CantidadCuotas { get; set; }
        public DateTime FechaPrimerVencimiento { get; set; }
        public string Periodicidad { get; set; } = null!;
        public string? Observaciones { get; set; }
        public List<CuotaResponse> Cuotas { get; set; } = new();
    }

    public class CuotaResponse
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
    }

    public class AjusteCuotaResponse
    {
        public int Id { get; set; }
        public int CuotaComercialId { get; set; }
        public int PlanPagoId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public TipoAjuste TipoAjuste { get; set; }
        public decimal? ImporteAnterior { get; set; }
        public decimal? ImporteNuevo { get; set; }
        public DateTime? FechaVencimientoAnterior { get; set; }
        public DateTime? FechaVencimientoNueva { get; set; }
        public string Motivo { get; set; } = null!;
        public DateTime FechaAjuste { get; set; }
        public string UsuarioAjuste { get; set; } = null!;
    }

    public class AjusteAcuerdoViaResponse
    {
        public int Id { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public decimal MontoAnterior { get; set; }
        public decimal MontoNuevo { get; set; }
        public decimal Diferencia { get; set; }
        public TipoAjusteVia TipoAjuste { get; set; }
        public string Motivo { get; set; } = null!;
        public DateTime FechaAjuste { get; set; }
        public string UsuarioAjuste { get; set; } = null!;
    }

    public class PagoComercialResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public int AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public DateTime FechaPago { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public decimal ImporteTotal { get; set; }
        public string MedioPago { get; set; } = null!;
        public TipoImputacion TipoImputacion { get; set; }
        public OrigenPago OrigenPago { get; set; }
        public string? Observaciones { get; set; }
        public PagoEstado Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public List<AplicacionPagoResponse> Aplicaciones { get; set; } = new();
    }

    public class AplicacionPagoResponse
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
    }

    public class HitoComercialResponse
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
    }

    public class EstadoComercialResponse
    {
        public int AcuerdoComercialId { get; set; }
        public int? AcuerdoComercialViaId { get; set; }
        public decimal TotalPrometido { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoRestante { get; set; }
    }

    public class SaldoComercialResponse
    {
        public string ExternoId { get; set; } = null!;
        public decimal TotalPrometido { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoRestante { get; set; }
    }

    public class ReporteComercialResumenResponse
    {
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public decimal TotalAcordadoActivo { get; set; }
        public decimal TotalCobradoPeriodo { get; set; }
        public decimal TotalPorCobrarPeriodo { get; set; }
        public decimal TotalVencido { get; set; }
        public decimal SaldoTotalClientes { get; set; }
        public int AcuerdosActivos { get; set; }
        public int CuotasPendientesPeriodo { get; set; }
        public int CuotasVencidas { get; set; }
        public List<ClienteDeudaReporteResponse> ClientesConDeuda { get; set; } = new();
        public List<CuotaReporteResponse> ProximosVencimientos { get; set; } = new();
    }

    public class ClienteDeudaReporteResponse
    {
        public string ClienteExternoId { get; set; } = null!;
        public decimal TotalAcordado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int AcuerdosActivos { get; set; }
    }

    public class CuotaReporteResponse
    {
        public int CuotaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public string NumeroAcuerdo { get; set; } = null!;
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public ViaOperacion ViaOperacion { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public DateTime FechaVencimiento { get; set; }
        public decimal SaldoPendiente { get; set; }
        public CuotaEstado Estado { get; set; }
    }
}

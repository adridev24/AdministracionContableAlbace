using BudgetControl.Api.DTOs.Commercial;

namespace BudgetControl.Api.Services.Commercial
{
    public interface IComercialService
    {
        Task<AcuerdoResponse> CreateAcuerdoAsync(CreateAcuerdoRequest request);
        Task<AcuerdoDetalleResponse?> GetAcuerdoDetalleAsync(int id);
        Task<AcuerdoResponse> AprobarAcuerdoAsync(int acuerdoId);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorClienteAsync(string clienteExternoId);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorObraAsync(string obraExternoId);
        Task<AcuerdoViaResponse> CrearViaAsync(int acuerdoId, CreateAcuerdoViaRequest request);
        Task<AcuerdoViaResponse> ModificarMontoViaAsync(int acuerdoViaId, ModificarMontoViaRequest request);
        Task<PlanPagoResponse> CrearPlanPagoAsync(int acuerdoViaId, CreatePlanPagoRequest request);
        Task<PlanPagoResponse> ActualizarPlanPagoAsync(int acuerdoViaId, UpdatePlanPagoRequest request);
        Task<EstadoComercialResponse> GetEstadoComercialAsync(int acuerdoId);
        Task<EstadoComercialResponse> GetEstadoComercialViaAsync(int acuerdoViaId);
        Task<SaldoComercialResponse> GetSaldoComercialClienteAsync(string clienteExternoId);
        Task<SaldoComercialResponse> GetSaldoComercialObraAsync(string obraExternaId);
        Task<ReporteComercialResumenResponse> GetReporteComercialResumenAsync(DateTime periodoDesde, DateTime periodoHasta, BudgetControl.Api.Models.Commercial.ViaOperacion? viaOperacion = null);
        Task<IEnumerable<CuotaResponse>> GetCuotasVencidasAsync();
        Task<IEnumerable<CuotaResponse>> GetCuotasPendientesAsync();
        Task<CuotaResponse> AjustarCuotaAsync(int cuotaId, AjusteCuotaRequest request);
        Task<CuotaResponse> AgregarCuotaAjusteAsync(int planPagoId, AddCuotaAjusteRequest request);
        Task<HitoComercialResponse> CrearHitoAsync(int acuerdoViaId, CreateHitoComercialRequest request);
        Task<IEnumerable<HitoComercialResponse>> GetHitosPorViaAsync(int acuerdoViaId);
        Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorCuotaAsync(int cuotaId);
        Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorAcuerdoAsync(int acuerdoId);
        Task<IEnumerable<AjusteAcuerdoViaResponse>> GetHistorialAjustesPorViaAsync(int acuerdoViaId);
    }
}

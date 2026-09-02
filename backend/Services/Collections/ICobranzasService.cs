using BudgetControl.Api.DTOs.Collections;

namespace BudgetControl.Api.Services.Collections
{
    public interface ICobranzasService
    {
        Task<IEnumerable<MedioPagoCobranzaResponse>> GetMediosPagoDisponiblesAsync(bool soloActivos = false);
        Task<IEnumerable<BancoCobranzaResponse>> GetBancosDisponiblesAsync(bool soloActivos = false);
        Task<CobranzaListResponse> GetCobranzasAsync(CobranzaListFilterRequest filter);
        Task<CobranzaResponse?> GetCobranzaAsync(int id);
        Task<CobranzaResponse> CreateCobranzaAsync(CobranzaHeaderRequest request);
        Task<CobranzaResponse> UpdateCobranzaAsync(int id, CobranzaHeaderRequest request);
        Task<IEnumerable<FacturaPendienteCobranzaResponse>> GetFacturasDisponiblesAsync(int cobranzaId);
        Task<IEnumerable<CobranzaAplicacionFacturaResponse>> GetAplicacionesFacturaAsync(int cobranzaId);
        Task<CobranzaResponse> AddMedioPagoAsync(int cobranzaId, CobranzaMedioPagoRequest request);
        Task<CobranzaResponse> UpdateMedioPagoAsync(int cobranzaId, int medioId, CobranzaMedioPagoRequest request);
        Task<CobranzaResponse> DeleteMedioPagoAsync(int cobranzaId, int medioId);
        Task<CobranzaResponse> AddAplicacionFacturaAsync(int cobranzaId, CobranzaAplicacionFacturaRequest request);
        Task<CobranzaResponse> UpdateAplicacionFacturaAsync(int cobranzaId, int aplicacionId, CobranzaAplicacionFacturaRequest request);
        Task<CobranzaResponse> DeleteAplicacionFacturaAsync(int cobranzaId, int aplicacionId);
        Task<CobranzaConfirmacionResponse> ConfirmarCobranzaAsync(int cobranzaId);
        Task<CobranzaAnulacionResponse> AnularCobranzaAsync(int cobranzaId, AnularCobranzaRequest request);
    }
}

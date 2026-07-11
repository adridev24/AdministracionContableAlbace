using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface IVentasService
    {
        Task<IEnumerable<TipoComprobanteVentaResponse>> GetTiposComprobanteAsync(bool soloActivos = false);
        Task<TipoComprobanteVentaResponse?> GetTipoComprobanteAsync(int id);
        Task<TipoComprobanteVentaResponse> CreateTipoComprobanteAsync(TipoComprobanteVentaRequest request);
        Task<TipoComprobanteVentaResponse> UpdateTipoComprobanteAsync(int id, TipoComprobanteVentaRequest request);
        Task<IEnumerable<PuntoVentaResponse>> GetPuntosVentaAsync(bool soloActivos = false);
        Task<PuntoVentaResponse?> GetPuntoVentaAsync(int id);
        Task<PuntoVentaResponse> CreatePuntoVentaAsync(PuntoVentaRequest request);
        Task<PuntoVentaResponse> UpdatePuntoVentaAsync(int id, PuntoVentaRequest request);
        Task<IEnumerable<PuntoVentaComprobanteResponse>> GetComprobantesPorPuntoVentaAsync(int puntoVentaId, bool soloActivos = false);
        Task<PuntoVentaComprobanteResponse> CreatePuntoVentaComprobanteAsync(int puntoVentaId, PuntoVentaComprobanteRequest request);
        Task<PuntoVentaComprobanteResponse> UpdatePuntoVentaComprobanteAsync(int puntoVentaId, int relacionId, PuntoVentaComprobanteRequest request);
        Task<VentaListResponse> GetVentasAsync(VentaListFilterRequest filters);
        Task<VentaResponse?> GetVentaAsync(int id);
        Task<VentaResponse> CreateVentaAsync(VentaHeaderRequest request);
        Task<VentaResponse> UpdateVentaAsync(int id, VentaHeaderRequest request);
    }
}

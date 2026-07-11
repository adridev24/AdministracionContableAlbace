using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface IVentasService
    {
        Task<IEnumerable<TipoComprobanteVentaResponse>> GetTiposComprobanteAsync(bool soloActivos = false);
        Task<VentaListResponse> GetVentasAsync(VentaListFilterRequest filters);
        Task<VentaResponse?> GetVentaAsync(int id);
        Task<VentaResponse> CreateVentaAsync(VentaHeaderRequest request);
        Task<VentaResponse> UpdateVentaAsync(int id, VentaHeaderRequest request);
    }
}

using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface IPercepcionIibbService
    {
        Task<ClientePercepcionIibbConfigResponse?> GetClienteConfigAsync(string clienteExternoId);
        Task<ClientePercepcionIibbConfigResponse> SaveClienteConfigAsync(ClientePercepcionIibbConfigRequest request);
        Task<VentaPercepcionIibbResponse?> GetPercepcionAsync(int ventaId);
        Task<VentaPercepcionIibbCalculoResponse> CalcularAsync(int ventaId);
    }
}

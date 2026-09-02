using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface ICuentaCorrienteClientesService
    {
        Task<CuentaCorrienteClienteResponse> GetCuentaCorrienteAsync(string clienteId, CuentaCorrienteClienteFilterRequest filter);
    }
}

using BudgetControl.Api.DTOs.Accounting;

namespace BudgetControl.Api.Services.Accounting
{
    public interface ICuentasContablesService
    {
        Task<IEnumerable<CuentaContableResponse>> GetCuentasAsync(CuentaContableFilter filter);
        Task<CuentaContableResponse?> GetCuentaAsync(int id);
        Task<CuentaContableResponse> CreateCuentaAsync(CreateCuentaContableRequest request);
        Task<CuentaContableResponse> UpdateCuentaAsync(int id, UpdateCuentaContableRequest request);
        Task<bool> DarDeBajaAsync(int id);
    }
}

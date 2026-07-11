using BudgetControl.Api.DTOs.Accounting;

namespace BudgetControl.Api.Services.Accounting
{
    public interface IAsientosContablesService
    {
        Task<IEnumerable<AsientoContableListResponse>> GetAsientosAsync(AsientoContableFilter filter);
        Task<AsientoContableResponse?> GetAsientoAsync(int id);
        Task<AsientoContableResponse> CrearAsientoManualAsync(CrearAsientoContableRequest request);
        Task<AsientoContableResponse> ReversarAsientoAsync(int id);
    }
}

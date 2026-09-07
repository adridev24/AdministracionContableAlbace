using BudgetControl.Api.DTOs.Treasury;

namespace BudgetControl.Api.Services.Treasury
{
    public interface ITesoreriaService
    {
        Task<IEnumerable<BancoResponse>> GetBancosAsync(BancoFilterRequest filter);
        Task<BancoResponse?> GetBancoAsync(int id);
        Task<BancoResponse> CreateBancoAsync(UpsertBancoRequest request);
        Task<BancoResponse> UpdateBancoAsync(int id, UpsertBancoRequest request);
        Task<IEnumerable<CuentaBancariaEmpresaResponse>> GetCuentasBancariasAsync(CuentaBancariaEmpresaFilterRequest filter);
        Task<CuentaBancariaEmpresaResponse?> GetCuentaBancariaAsync(int id);
        Task<CuentaBancariaEmpresaResponse> CreateCuentaBancariaAsync(UpsertCuentaBancariaEmpresaRequest request);
        Task<CuentaBancariaEmpresaResponse> UpdateCuentaBancariaAsync(int id, UpsertCuentaBancariaEmpresaRequest request);
    }
}

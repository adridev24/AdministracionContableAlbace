using BudgetControl.Api.DTOs.Accounting;

namespace BudgetControl.Api.Services.Accounting
{
    public interface IConfiguracionesContablesService
    {
        Task<IEnumerable<TipoOperacionContableResponse>> GetTiposOperacionAsync();
        Task<IEnumerable<ConfiguracionContableListResponse>> GetConfiguracionesAsync(ConfiguracionContableFilter filter);
        Task<ConfiguracionContableResponse?> GetConfiguracionAsync(int id);
        Task<ConfiguracionContableResponse?> GetConfiguracionPorOperacionAsync(string codigoOperacion);
        Task<ConfiguracionContableResponse> CreateConfiguracionAsync(UpsertConfiguracionContableRequest request);
        Task<ConfiguracionContableResponse> UpdateConfiguracionAsync(int id, UpsertConfiguracionContableRequest request);
        Task<bool> DarDeBajaAsync(int id);
    }
}

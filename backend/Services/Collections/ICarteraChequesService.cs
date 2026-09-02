using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Models.Collections;

namespace BudgetControl.Api.Services.Collections
{
    public interface ICarteraChequesService
    {
        Task<IEnumerable<ChequeTerceroListResponse>> GetChequesAsync(CarteraChequesFilterRequest filter);
        Task<ChequeTerceroDetalleResponse?> GetChequeAsync(int id);
        Task<ChequeTerceroDetalleResponse> DepositarAsync(int id, DepositarChequeTerceroRequest request);
        Task<ChequeTerceroDetalleResponse> AcreditarAsync(int id, AcreditarChequeTerceroRequest request);
        Task EnsureChequesDesdeCobranzaConfirmadaAsync(Cobranza cobranza);
    }
}

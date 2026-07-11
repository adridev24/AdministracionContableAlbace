using BudgetControl.Api.DTOs.Accounting;

namespace BudgetControl.Api.Services.Accounting
{
    public interface IContabilizacionAutomaticaService
    {
        Task<ContabilizacionAutomaticaResponse> GenerarAsientoAutomaticoAsync(SolicitudContabilizacionAutomaticaRequest request);
    }
}

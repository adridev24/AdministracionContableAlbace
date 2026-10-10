using BudgetControl.Api.DTOs.Commercial;

namespace BudgetControl.Api.Services.Commercial;

public interface IRevisionPlanPagoService
{
    Task<PreparacionRevisionPlanResponse> PrepararAsync(int acuerdoId, int viaId, int planId, CancellationToken ct = default);
    Task<ConfirmacionRevisionPlanResponse> ConfirmarAsync(RevisionPlanPagoRequest request, CancellationToken ct = default);
}

public sealed class RevisionPlanConflictException : InvalidOperationException
{
    public RevisionPlanConflictException(string message, Exception? inner = null) : base(message, inner) { }
}

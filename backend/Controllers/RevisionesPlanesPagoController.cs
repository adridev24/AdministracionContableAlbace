using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/comercial/acuerdos-vias/{viaId:int}/plan-pago/revision")]
public sealed class RevisionesPlanesPagoController : ControllerBase
{
    private readonly IRevisionPlanPagoService _service;
    public RevisionesPlanesPagoController(IRevisionPlanPagoService service) => _service = service;

    [HttpGet]
    public Task<IActionResult> Preparar(int viaId, [FromQuery] int acuerdoId, [FromQuery] int planId, CancellationToken ct)
        => Execute(async () => await _service.PrepararAsync(acuerdoId, viaId, planId, ct));

    [HttpPost]
    public Task<IActionResult> Confirmar(int viaId, [FromBody] RevisionPlanPagoRequest request, CancellationToken ct)
        => Execute(async () =>
        {
            if (viaId != request.AcuerdoComercialViaId) throw new InvalidOperationException("La vía de la ruta no coincide con la solicitud.");
            return await _service.ConfirmarAsync(request, ct);
        });

    private async Task<IActionResult> Execute(Func<Task<object>> action)
    {
        try { return Ok(await action()); }
        catch (RevisionPlanConflictException ex) { return Conflict(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException)
        { return User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

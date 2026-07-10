using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/comercial")]
    public class ComercialAjustesController : ControllerBase
    {
        private readonly IComercialService _service;

        public ComercialAjustesController(IComercialService service)
        {
            _service = service;
        }

        [HttpPut("cuotas/{cuotaId}/ajustar")]
        public async Task<IActionResult> AjustarCuota(int cuotaId, [FromBody] AjusteCuotaRequest request)
        {
            try
            {
                var resultado = await _service.AjustarCuotaAsync(cuotaId, request);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("planes/{planPagoId}/cuotas-ajuste")]
        public async Task<IActionResult> AgregarCuota(int planPagoId, [FromBody] AddCuotaAjusteRequest request)
        {
            try
            {
                var resultado = await _service.AgregarCuotaAjusteAsync(planPagoId, request);
                return CreatedAtAction(nameof(AjustarCuota), new { cuotaId = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("cuotas/{cuotaId}/historial-ajustes")]
        public async Task<IActionResult> GetHistorialCuota(int cuotaId)
        {
            return Ok(await _service.GetHistorialAjustesPorCuotaAsync(cuotaId));
        }

        [HttpGet("acuerdos/{acuerdoId}/historial-ajustes")]
        public async Task<IActionResult> GetHistorialAcuerdo(int acuerdoId)
        {
            return Ok(await _service.GetHistorialAjustesPorAcuerdoAsync(acuerdoId));
        }

        [HttpGet("acuerdos-vias/{acuerdoViaId}/historial-ajustes")]
        public async Task<IActionResult> GetHistorialVia(int acuerdoViaId)
        {
            return Ok(await _service.GetHistorialAjustesPorViaAsync(acuerdoViaId));
        }
    }
}

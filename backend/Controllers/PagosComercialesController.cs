using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/pagos-comerciales")]
    public class PagosComercialesController : ControllerBase
    {
        private readonly IPagoComercialService _service;

        public PagosComercialesController(IPagoComercialService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreatePagoComercialRequest request)
        {
            try
            {
                var resultado = await _service.RegistrarPagoAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/api/comercial/acuerdos-vias/{id}/pagos")]
        public async Task<IActionResult> RegisterByVia(int id, [FromBody] CreatePagoComercialRequest request)
        {
            try
            {
                request.AcuerdoComercialViaId = id;
                var resultado = await _service.RegistrarPagoAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/aplicar")]
        public async Task<IActionResult> Apply(int id, [FromBody] AplicarPagoRequest request)
        {
            try
            {
                return Ok(await _service.AplicarPagoAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/anular")]
        public async Task<IActionResult> Anular(int id, [FromBody] AnularPagoComercialRequest request)
        {
            try
            {
                return Ok(await _service.AnularPagoAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("cuota/{cuotaId}/aplicaciones")]
        public async Task<IActionResult> GetAplicacionesPorCuota(int cuotaId)
        {
            return Ok(await _service.GetAplicacionesPorCuotaAsync(cuotaId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pago = await _service.GetPagoAsync(id);
            return pago == null ? NotFound() : Ok(pago);
        }
    }
}

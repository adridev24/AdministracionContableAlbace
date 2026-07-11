using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Services.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/contabilidad/asientos")]
    public class AsientosContablesController : ControllerBase
    {
        private readonly IAsientosContablesService _service;

        public AsientosContablesController(IAsientosContablesService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] AsientoContableFilter filter)
        {
            return Ok(await _service.GetAsientosAsync(filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var asiento = await _service.GetAsientoAsync(id);
            return asiento == null ? NotFound(new { error = "Asiento contable no encontrado." }) : Ok(asiento);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearAsientoContableRequest request)
        {
            try
            {
                var asiento = await _service.CrearAsientoManualAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = asiento.Id }, asiento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/reversar")]
        public async Task<IActionResult> Reversar(int id)
        {
            try
            {
                return Ok(await _service.ReversarAsientoAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

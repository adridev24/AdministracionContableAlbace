using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Services.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/contabilidad/cuentas")]
    public class CuentasContablesController : ControllerBase
    {
        private readonly ICuentasContablesService _service;

        public CuentasContablesController(ICuentasContablesService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CuentaContableFilter filter)
        {
            return Ok(await _service.GetCuentasAsync(filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cuenta = await _service.GetCuentaAsync(id);
            return cuenta == null ? NotFound(new { error = "Cuenta contable no encontrada." }) : Ok(cuenta);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCuentaContableRequest request)
        {
            try
            {
                var cuenta = await _service.CreateCuentaAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = cuenta.Id }, cuenta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCuentaContableRequest request)
        {
            try
            {
                return Ok(await _service.UpdateCuentaAsync(id, request));
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var updated = await _service.DarDeBajaAsync(id);
            return updated ? NoContent() : NotFound(new { error = "Cuenta contable no encontrada." });
        }
    }
}

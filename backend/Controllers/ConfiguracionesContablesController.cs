using BudgetControl.Api.DTOs.Accounting;
using BudgetControl.Api.Services.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/contabilidad/configuraciones")]
    public class ConfiguracionesContablesController : ControllerBase
    {
        private readonly IConfiguracionesContablesService _service;

        public ConfiguracionesContablesController(IConfiguracionesContablesService service)
        {
            _service = service;
        }

        [HttpGet("tipos-operacion")]
        public async Task<IActionResult> GetTiposOperacion()
        {
            return Ok(await _service.GetTiposOperacionAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ConfiguracionContableFilter filter)
        {
            return Ok(await _service.GetConfiguracionesAsync(filter));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var configuracion = await _service.GetConfiguracionAsync(id);
            return configuracion == null ? NotFound(new { error = "Configuracion contable no encontrada." }) : Ok(configuracion);
        }

        [HttpGet("operacion/{codigoOperacion}")]
        public async Task<IActionResult> GetByOperacion(string codigoOperacion)
        {
            try
            {
                var configuracion = await _service.GetConfiguracionPorOperacionAsync(codigoOperacion);
                return configuracion == null ? NotFound(new { error = "Configuracion contable no encontrada." }) : Ok(configuracion);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertConfiguracionContableRequest request)
        {
            try
            {
                var configuracion = await _service.CreateConfiguracionAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = configuracion.Id }, configuracion);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertConfiguracionContableRequest request)
        {
            try
            {
                return Ok(await _service.UpdateConfiguracionAsync(id, request));
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var updated = await _service.DarDeBajaAsync(id);
            return updated ? NoContent() : NotFound(new { error = "Configuracion contable no encontrada." });
        }
    }
}

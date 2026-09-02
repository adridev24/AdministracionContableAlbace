using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers.Sales
{
    [ApiController]
    [Authorize]
    [Route("api/cuenta-corriente-clientes")]
    public class CuentaCorrienteClientesController : ControllerBase
    {
        private readonly ICuentaCorrienteClientesService _service;

        public CuentaCorrienteClientesController(ICuentaCorrienteClientesService service)
        {
            _service = service;
        }

        [HttpGet("{clienteId}")]
        public async Task<IActionResult> GetCuentaCorriente(string clienteId, [FromQuery] CuentaCorrienteClienteFilterRequest filter)
        {
            try
            {
                return Ok(await _service.GetCuentaCorrienteAsync(clienteId, filter));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

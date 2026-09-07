using BudgetControl.Api.DTOs.Treasury;
using BudgetControl.Api.Services.Treasury;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers.Treasury
{
    [ApiController]
    [Authorize]
    [Route("api/tesoreria")]
    public class TesoreriaController : ControllerBase
    {
        private readonly ITesoreriaService _service;

        public TesoreriaController(ITesoreriaService service)
        {
            _service = service;
        }

        [HttpGet("bancos")]
        public async Task<IActionResult> GetBancos([FromQuery] BancoFilterRequest filter)
        {
            return Ok(await _service.GetBancosAsync(filter));
        }

        [HttpGet("bancos/{id}")]
        public async Task<IActionResult> GetBanco(int id)
        {
            var banco = await _service.GetBancoAsync(id);
            return banco == null ? NotFound(new { error = "Banco no encontrado." }) : Ok(banco);
        }

        [HttpPost("bancos")]
        public async Task<IActionResult> CreateBanco([FromBody] UpsertBancoRequest request)
        {
            try
            {
                var banco = await _service.CreateBancoAsync(request);
                return CreatedAtAction(nameof(GetBanco), new { id = banco.Id }, banco);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("bancos/{id}")]
        public async Task<IActionResult> UpdateBanco(int id, [FromBody] UpsertBancoRequest request)
        {
            try
            {
                return Ok(await _service.UpdateBancoAsync(id, request));
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

        [HttpGet("cuentas-bancarias")]
        public async Task<IActionResult> GetCuentasBancarias([FromQuery] CuentaBancariaEmpresaFilterRequest filter)
        {
            return Ok(await _service.GetCuentasBancariasAsync(filter));
        }

        [HttpGet("cuentas-bancarias/{id}")]
        public async Task<IActionResult> GetCuentaBancaria(int id)
        {
            var cuenta = await _service.GetCuentaBancariaAsync(id);
            return cuenta == null ? NotFound(new { error = "Cuenta bancaria no encontrada." }) : Ok(cuenta);
        }

        [HttpPost("cuentas-bancarias")]
        public async Task<IActionResult> CreateCuentaBancaria([FromBody] UpsertCuentaBancariaEmpresaRequest request)
        {
            try
            {
                var cuenta = await _service.CreateCuentaBancariaAsync(request);
                return CreatedAtAction(nameof(GetCuentaBancaria), new { id = cuenta.Id }, cuenta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("cuentas-bancarias/{id}")]
        public async Task<IActionResult> UpdateCuentaBancaria(int id, [FromBody] UpsertCuentaBancariaEmpresaRequest request)
        {
            try
            {
                return Ok(await _service.UpdateCuentaBancariaAsync(id, request));
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

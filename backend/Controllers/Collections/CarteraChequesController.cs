using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Services.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace BudgetControl.Api.Controllers.Collections
{
    [ApiController]
    [Authorize]
    [Route("api/cartera-cheques")]
    public class CarteraChequesController : ControllerBase
    {
        private readonly ICarteraChequesService _service;

        public CarteraChequesController(ICarteraChequesService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCheques([FromQuery] CarteraChequesFilterRequest filter)
        {
            try
            {
                return Ok(await _service.GetChequesAsync(filter));
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
            {
                return BadRequest(new { error = "La migracion de Cartera de Cheques no esta aplicada en la base de datos." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCheque(int id)
        {
            try
            {
                var cheque = await _service.GetChequeAsync(id);
                return cheque == null ? NotFound(new { error = "Cheque no encontrado." }) : Ok(cheque);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable || ex.SqlState == PostgresErrorCodes.UndefinedColumn)
            {
                return BadRequest(new { error = "La migracion de Cartera de Cheques no esta aplicada en la base de datos." });
            }
        }

        [HttpPost("{id}/depositar")]
        public async Task<IActionResult> Depositar(int id, [FromBody] DepositarChequeTerceroRequest request)
        {
            try
            {
                return Ok(await _service.DepositarAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/acreditar")]
        public async Task<IActionResult> Acreditar(int id, [FromBody] AcreditarChequeTerceroRequest request)
        {
            try
            {
                return Ok(await _service.AcreditarAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

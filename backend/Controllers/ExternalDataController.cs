using Microsoft.AspNetCore.Mvc;
using BudgetControl.Api.Services;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalDataController : ControllerBase
    {
        private readonly IExternalDataService _externalService;

        public ExternalDataController(IExternalDataService externalService)
        {
            _externalService = externalService;
        }

        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            try
            {
                var clients = await _externalService.GetClientsAsync();
                return Ok(clients);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "External SQL Server unavailable", detail = ex.Message });
            }
        }

        [HttpGet("clients/{id}")]
        public async Task<IActionResult> GetClientById(int id)
        {
            try
            {
                var client = await _externalService.GetClientByIdAsync(id);
                if (client == null) return NotFound();
                return Ok(client);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "External SQL Server unavailable", detail = ex.Message });
            }
        }

        [HttpGet("clients/{id}/obras")]
        public async Task<IActionResult> GetObrasByClient(int id)
        {
            try
            {
                var obras = await _externalService.GetObrasByClientAsync(id);
                return Ok(obras);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "External SQL Server unavailable", detail = ex.Message });
            }
        }

        [HttpGet("obras/{id}")]
        public async Task<IActionResult> GetObraById(int id)
        {
            try
            {
                var obra = await _externalService.GetObraByIdAsync(id);
                if (obra == null) return NotFound();
                return Ok(obra);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "External SQL Server unavailable", detail = ex.Message });
            }
        }

        [HttpGet("obras")]
        public async Task<IActionResult> GetObras()
        {
            try
            {
                var obras = await _externalService.GetObrasAsync();
                return Ok(obras);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "External SQL Server unavailable", detail = ex.Message });
            }
        }
    }
}

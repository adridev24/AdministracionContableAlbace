using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/cuotas")]
    public class CuotasController : ControllerBase
    {
        private readonly IComercialService _service;

        public CuotasController(IComercialService service)
        {
            _service = service;
        }

        [HttpGet("vencidas")]
        public async Task<IActionResult> GetVencidas()
        {
            return Ok(await _service.GetCuotasVencidasAsync());
        }

        [HttpGet("pendientes")]
        public async Task<IActionResult> GetPendientes()
        {
            return Ok(await _service.GetCuotasPendientesAsync());
        }
    }
}

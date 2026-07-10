using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/obras")]
    public class ObrasController : ControllerBase
    {
        private readonly IComercialService _service;

        public ObrasController(IComercialService service)
        {
            _service = service;
        }

        [HttpGet("{obraExternaId}/saldo-comercial")]
        public async Task<IActionResult> GetSaldoComercial(string obraExternaId)
        {
            return Ok(await _service.GetSaldoComercialObraAsync(obraExternaId));
        }
    }
}

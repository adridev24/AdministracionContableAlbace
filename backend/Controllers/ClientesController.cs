using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly IComercialService _service;

        public ClientesController(IComercialService service)
        {
            _service = service;
        }

        [HttpGet("{clienteExternoId}/saldo-comercial")]
        public async Task<IActionResult> GetSaldoComercial(string clienteExternoId)
        {
            return Ok(await _service.GetSaldoComercialClienteAsync(clienteExternoId));
        }
    }
}

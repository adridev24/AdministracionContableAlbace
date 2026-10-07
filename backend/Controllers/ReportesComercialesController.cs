using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/comercial/reportes")]
    public class ReportesComercialesController : ControllerBase
    {
        private readonly IComercialService _service;

        public ReportesComercialesController(IComercialService service)
        {
            _service = service;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? via)
        {
            if (desde.HasValue != hasta.HasValue)
                return BadRequest(new { error = "Debe indicar ambas fechas del período." });
            ViaOperacion? viaOperacion = null;

            if (!string.IsNullOrWhiteSpace(via) && !via.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<ViaOperacion>(via, true, out var parsedVia) || !Enum.IsDefined(parsedVia))
                {
                    return BadRequest(new { error = "La vía indicada no es válida." });
                }

                viaOperacion = parsedVia;
            }

            if (hasta?.Date < desde?.Date)
            {
                return BadRequest(new { error = "La fecha hasta no puede ser anterior a la fecha desde." });
            }

            return Ok(await _service.GetReporteComercialResumenAsync(desde, hasta, viaOperacion));
        }
    }
}

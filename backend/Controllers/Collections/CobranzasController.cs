using BudgetControl.Api.DTOs.Collections;
using BudgetControl.Api.Services.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers.Collections
{
    [ApiController]
    [Authorize]
    [Route("api/cobranzas")]
    public class CobranzasController : ControllerBase
    {
        private readonly ICobranzasService _service;

        public CobranzasController(ICobranzasService service)
        {
            _service = service;
        }

        [HttpGet("medios-pago")]
        public async Task<IActionResult> GetMediosPago([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetMediosPagoDisponiblesAsync(soloActivos));
        }

        [HttpGet("bancos")]
        public async Task<IActionResult> GetBancos([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetBancosDisponiblesAsync(soloActivos));
        }

        [HttpGet]
        public async Task<IActionResult> GetCobranzas([FromQuery] CobranzaListFilterRequest filter)
        {
            return Ok(await _service.GetCobranzasAsync(filter));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCobranza(int id)
        {
            var cobranza = await _service.GetCobranzaAsync(id);
            return cobranza == null ? NotFound(new { error = "Cobranza no encontrada." }) : Ok(cobranza);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCobranza([FromBody] CobranzaHeaderRequest request)
        {
            try
            {
                var cobranza = await _service.CreateCobranzaAsync(request);
                return CreatedAtAction(nameof(GetCobranza), new { id = cobranza.Id }, cobranza);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCobranza(int id, [FromBody] CobranzaHeaderRequest request)
        {
            try
            {
                return Ok(await _service.UpdateCobranzaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/facturas-disponibles")]
        public async Task<IActionResult> GetFacturasDisponibles(int id)
        {
            try
            {
                return Ok(await _service.GetFacturasDisponiblesAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/medios")]
        public async Task<IActionResult> AddMedioPago(int id, [FromBody] CobranzaMedioPagoRequest request)
        {
            try
            {
                return Ok(await _service.AddMedioPagoAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/medios/{medioId}")]
        public async Task<IActionResult> UpdateMedioPago(int id, int medioId, [FromBody] CobranzaMedioPagoRequest request)
        {
            try
            {
                return Ok(await _service.UpdateMedioPagoAsync(id, medioId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}/medios/{medioId}")]
        public async Task<IActionResult> DeleteMedioPago(int id, int medioId)
        {
            try
            {
                return Ok(await _service.DeleteMedioPagoAsync(id, medioId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/aplicaciones")]
        public async Task<IActionResult> AddAplicacionFactura(int id, [FromBody] CobranzaAplicacionFacturaRequest request)
        {
            try
            {
                return Ok(await _service.AddAplicacionFacturaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/aplicaciones")]
        public async Task<IActionResult> GetAplicacionesFactura(int id)
        {
            try
            {
                return Ok(await _service.GetAplicacionesFacturaAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/aplicaciones/{aplicacionId}")]
        public async Task<IActionResult> UpdateAplicacionFactura(int id, int aplicacionId, [FromBody] CobranzaAplicacionFacturaRequest request)
        {
            try
            {
                return Ok(await _service.UpdateAplicacionFacturaAsync(id, aplicacionId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}/aplicaciones/{aplicacionId}")]
        public async Task<IActionResult> DeleteAplicacionFactura(int id, int aplicacionId)
        {
            try
            {
                return Ok(await _service.DeleteAplicacionFacturaAsync(id, aplicacionId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarCobranza(int id)
        {
            try
            {
                return Ok(await _service.ConfirmarCobranzaAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

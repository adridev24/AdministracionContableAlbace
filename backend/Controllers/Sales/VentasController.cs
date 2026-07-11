using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers.Sales
{
    [ApiController]
    [Authorize]
    [Route("api/ventas")]
    public class VentasController : ControllerBase
    {
        private readonly IVentasService _service;

        public VentasController(IVentasService service)
        {
            _service = service;
        }

        [HttpGet("tipos-comprobante")]
        public async Task<IActionResult> GetTiposComprobante([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetTiposComprobanteAsync(soloActivos));
        }

        [HttpGet("tipos-comprobante/{id}")]
        public async Task<IActionResult> GetTipoComprobante(int id)
        {
            var tipo = await _service.GetTipoComprobanteAsync(id);
            return tipo == null ? NotFound() : Ok(tipo);
        }

        [HttpPost("tipos-comprobante")]
        public async Task<IActionResult> CreateTipoComprobante([FromBody] TipoComprobanteVentaRequest request)
        {
            try
            {
                var tipo = await _service.CreateTipoComprobanteAsync(request);
                return CreatedAtAction(nameof(GetTipoComprobante), new { id = tipo.Id }, tipo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("tipos-comprobante/{id}")]
        public async Task<IActionResult> UpdateTipoComprobante(int id, [FromBody] TipoComprobanteVentaRequest request)
        {
            try
            {
                return Ok(await _service.UpdateTipoComprobanteAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("configuraciones-comprobante")]
        public async Task<IActionResult> GetConfiguracionesComprobante([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetTiposComprobanteAsync(soloActivos));
        }

        [HttpGet("configuraciones-comprobante/{id}")]
        public async Task<IActionResult> GetConfiguracionComprobante(int id)
        {
            return await GetTipoComprobante(id);
        }

        [HttpPost("configuraciones-comprobante")]
        public async Task<IActionResult> CreateConfiguracionComprobante([FromBody] TipoComprobanteVentaRequest request)
        {
            return await CreateTipoComprobante(request);
        }

        [HttpPut("configuraciones-comprobante/{id}")]
        public async Task<IActionResult> UpdateConfiguracionComprobante(int id, [FromBody] TipoComprobanteVentaRequest request)
        {
            return await UpdateTipoComprobante(id, request);
        }

        [HttpGet("puntos-venta")]
        public async Task<IActionResult> GetPuntosVenta([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetPuntosVentaAsync(soloActivos));
        }

        [HttpGet("puntos-venta/{id}")]
        public async Task<IActionResult> GetPuntoVenta(int id)
        {
            var punto = await _service.GetPuntoVentaAsync(id);
            return punto == null ? NotFound() : Ok(punto);
        }

        [HttpPost("puntos-venta")]
        public async Task<IActionResult> CreatePuntoVenta([FromBody] PuntoVentaRequest request)
        {
            try
            {
                var punto = await _service.CreatePuntoVentaAsync(request);
                return CreatedAtAction(nameof(GetPuntoVenta), new { id = punto.Id }, punto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("puntos-venta/{id}")]
        public async Task<IActionResult> UpdatePuntoVenta(int id, [FromBody] PuntoVentaRequest request)
        {
            try
            {
                return Ok(await _service.UpdatePuntoVentaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("puntos-venta/{id}/comprobantes")]
        public async Task<IActionResult> GetComprobantesPorPuntoVenta(int id, [FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetComprobantesPorPuntoVentaAsync(id, soloActivos));
        }

        [HttpPost("puntos-venta/{id}/comprobantes")]
        public async Task<IActionResult> CreatePuntoVentaComprobante(int id, [FromBody] PuntoVentaComprobanteRequest request)
        {
            try
            {
                var relacion = await _service.CreatePuntoVentaComprobanteAsync(id, request);
                return CreatedAtAction(nameof(GetComprobantesPorPuntoVenta), new { id }, relacion);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("puntos-venta/{id}/comprobantes/{relacionId}")]
        public async Task<IActionResult> UpdatePuntoVentaComprobante(int id, int relacionId, [FromBody] PuntoVentaComprobanteRequest request)
        {
            try
            {
                return Ok(await _service.UpdatePuntoVentaComprobanteAsync(id, relacionId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVentas([FromQuery] VentaListFilterRequest filters)
        {
            return Ok(await _service.GetVentasAsync(filters));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVenta(int id)
        {
            var venta = await _service.GetVentaAsync(id);
            return venta == null ? NotFound() : Ok(venta);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVenta([FromBody] VentaHeaderRequest request)
        {
            try
            {
                var venta = await _service.CreateVentaAsync(request);
                return CreatedAtAction(nameof(GetVenta), new { id = venta.Id }, venta);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVenta(int id, [FromBody] VentaHeaderRequest request)
        {
            try
            {
                return Ok(await _service.UpdateVentaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

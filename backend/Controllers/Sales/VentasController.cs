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

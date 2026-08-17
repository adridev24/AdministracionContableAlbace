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
        private readonly IPercepcionIibbService _percepcionIibbService;

        public VentasController(IVentasService service, IPercepcionIibbService percepcionIibbService)
        {
            _service = service;
            _percepcionIibbService = percepcionIibbService;
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

        [HttpGet("configuraciones-comprobante/{id}/puntos-venta")]
        public async Task<IActionResult> GetPuntosVentaPorComprobante(int id, [FromQuery] int? relacionActualId = null)
        {
            try
            {
                return Ok(await _service.GetPuntosVentaPorComprobanteAsync(id, relacionActualId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
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

        [HttpGet("alicuotas-iva")]
        public async Task<IActionResult> GetAlicuotasIva([FromQuery] bool soloActivos = false, [FromQuery] string? search = null)
        {
            return Ok(await _service.GetAlicuotasIvaAsync(soloActivos, search));
        }

        [HttpGet("alicuotas-iva/{id}")]
        public async Task<IActionResult> GetAlicuotaIva(int id)
        {
            var item = await _service.GetAlicuotaIvaAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("alicuotas-iva")]
        public async Task<IActionResult> CreateAlicuotaIva([FromBody] AlicuotaIvaVentaRequest request)
        {
            try
            {
                var item = await _service.CreateAlicuotaIvaAsync(request);
                return CreatedAtAction(nameof(GetAlicuotaIva), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("alicuotas-iva/{id}")]
        public async Task<IActionResult> UpdateAlicuotaIva(int id, [FromBody] AlicuotaIvaVentaRequest request)
        {
            try
            {
                return Ok(await _service.UpdateAlicuotaIvaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("nomencladores")]
        public async Task<IActionResult> GetNomencladores([FromQuery] bool soloActivos = false, [FromQuery] string? search = null)
        {
            return Ok(await _service.GetNomencladoresFceAsync(soloActivos, search));
        }

        [HttpGet("nomencladores/{id}")]
        public async Task<IActionResult> GetNomenclador(int id)
        {
            var item = await _service.GetNomencladorFceAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("nomencladores")]
        public async Task<IActionResult> CreateNomenclador([FromBody] NomencladorFceRequest request)
        {
            try
            {
                var item = await _service.CreateNomencladorFceAsync(request);
                return CreatedAtAction(nameof(GetNomenclador), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("nomencladores/{id}")]
        public async Task<IActionResult> UpdateNomenclador(int id, [FromBody] NomencladorFceRequest request)
        {
            try
            {
                return Ok(await _service.UpdateNomencladorFceAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("percepciones-iibb")]
        public async Task<IActionResult> GetPercepcionesIibb([FromQuery] bool soloActivos = false, [FromQuery] string? search = null, [FromQuery] bool? soloVigentes = null)
        {
            return Ok(await _service.GetPercepcionesIibbAsync(soloActivos, search, soloVigentes));
        }

        [HttpGet("percepciones-iibb/{id}")]
        public async Task<IActionResult> GetPercepcionIibb(int id)
        {
            var item = await _service.GetPercepcionIibbAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("percepciones-iibb")]
        public async Task<IActionResult> CreatePercepcionIibb([FromBody] PercepcionIibbEntreRiosRequest request)
        {
            try
            {
                var item = await _service.CreatePercepcionIibbAsync(request);
                return CreatedAtAction(nameof(GetPercepcionIibb), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("percepciones-iibb/{id}")]
        public async Task<IActionResult> UpdatePercepcionIibb(int id, [FromBody] PercepcionIibbEntreRiosRequest request)
        {
            try
            {
                return Ok(await _service.UpdatePercepcionIibbAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("clientes/{clienteExternoId}/percepcion-iibb/configuracion")]
        public async Task<IActionResult> GetClientePercepcionIibbConfig(string clienteExternoId)
        {
            var config = await _percepcionIibbService.GetClienteConfigAsync(clienteExternoId);
            return config == null ? NotFound() : Ok(config);
        }

        [HttpPut("clientes/{clienteExternoId}/percepcion-iibb/configuracion")]
        public async Task<IActionResult> SaveClientePercepcionIibbConfig(string clienteExternoId, [FromBody] ClientePercepcionIibbConfigRequest request)
        {
            try
            {
                request.ClienteExternoId = clienteExternoId;
                return Ok(await _percepcionIibbService.SaveClienteConfigAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/percepciones-iibb")]
        public async Task<IActionResult> GetVentaPercepcionIibb(int id)
        {
            var percepcion = await _percepcionIibbService.GetPercepcionAsync(id);
            return percepcion == null ? NotFound() : Ok(percepcion);
        }

        [HttpPost("{id}/percepciones-iibb/calcular")]
        public async Task<IActionResult> CalcularVentaPercepcionIibb(int id)
        {
            try
            {
                var result = await _percepcionIibbService.CalcularAsync(id);
                result.Venta = await _service.GetVentaAsync(id);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("categorias-items")]
        public async Task<IActionResult> GetCategoriasItems([FromQuery] bool soloActivos = false, [FromQuery] string? search = null)
        {
            return Ok(await _service.GetCategoriasItemsFacturablesAsync(soloActivos, search));
        }

        [HttpGet("categorias-items/{id}")]
        public async Task<IActionResult> GetCategoriaItem(int id)
        {
            var item = await _service.GetCategoriaItemFacturableAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("categorias-items")]
        public async Task<IActionResult> CreateCategoriaItem([FromBody] CategoriaItemFacturableRequest request)
        {
            try
            {
                var item = await _service.CreateCategoriaItemFacturableAsync(request);
                return CreatedAtAction(nameof(GetCategoriaItem), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("categorias-items/{id}")]
        public async Task<IActionResult> UpdateCategoriaItem(int id, [FromBody] CategoriaItemFacturableRequest request)
        {
            try
            {
                return Ok(await _service.UpdateCategoriaItemFacturableAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("unidades-medida")]
        public async Task<IActionResult> GetUnidadesMedida([FromQuery] bool soloActivos = false, [FromQuery] string? search = null)
        {
            return Ok(await _service.GetUnidadesMedidaAsync(soloActivos, search));
        }

        [HttpGet("unidades-medida/{id}")]
        public async Task<IActionResult> GetUnidadMedida(int id)
        {
            var item = await _service.GetUnidadMedidaAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("unidades-medida")]
        public async Task<IActionResult> CreateUnidadMedida([FromBody] UnidadMedidaVentaRequest request)
        {
            try
            {
                var item = await _service.CreateUnidadMedidaAsync(request);
                return CreatedAtAction(nameof(GetUnidadMedida), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("unidades-medida/{id}")]
        public async Task<IActionResult> UpdateUnidadMedida(int id, [FromBody] UnidadMedidaVentaRequest request)
        {
            try
            {
                return Ok(await _service.UpdateUnidadMedidaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("items-facturables")]
        public async Task<IActionResult> GetItemsFacturables(
            [FromQuery] bool soloActivos = false,
            [FromQuery] string? search = null,
            [FromQuery] int? categoriaId = null,
            [FromQuery] int? unidadMedidaId = null,
            [FromQuery] int? tratamientoIvaId = null,
            [FromQuery] int? nomencladorId = null)
        {
            return Ok(await _service.GetItemsFacturablesAsync(soloActivos, search, categoriaId, unidadMedidaId, tratamientoIvaId, nomencladorId));
        }

        [HttpGet("items-facturables/{id}")]
        public async Task<IActionResult> GetItemFacturable(int id)
        {
            var item = await _service.GetItemFacturableAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("items-facturables")]
        public async Task<IActionResult> CreateItemFacturable([FromBody] ItemFacturableRequest request)
        {
            try
            {
                var item = await _service.CreateItemFacturableAsync(request);
                return CreatedAtAction(nameof(GetItemFacturable), new { id = item.Id }, item);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("items-facturables/{id}")]
        public async Task<IActionResult> UpdateItemFacturable(int id, [FromBody] ItemFacturableRequest request)
        {
            try
            {
                return Ok(await _service.UpdateItemFacturableAsync(id, request));
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

        [HttpGet("{ventaId}/detalles")]
        public async Task<IActionResult> GetDetalles(int ventaId)
        {
            try
            {
                return Ok(await _service.GetDetallesAsync(ventaId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{ventaId}/detalles")]
        public async Task<IActionResult> CreateDetalle(int ventaId, [FromBody] VentaDetalleRequest request)
        {
            try
            {
                return Ok(await _service.CreateDetalleAsync(ventaId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{ventaId}/detalles/{detalleId}")]
        public async Task<IActionResult> UpdateDetalle(int ventaId, int detalleId, [FromBody] VentaDetalleRequest request)
        {
            try
            {
                return Ok(await _service.UpdateDetalleAsync(ventaId, detalleId, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{ventaId}/detalles/{detalleId}")]
        public async Task<IActionResult> DeleteDetalle(int ventaId, int detalleId)
        {
            try
            {
                return Ok(await _service.DeleteDetalleAsync(ventaId, detalleId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("facturas/{facturaId}/validacion-confirmacion")]
        public async Task<IActionResult> ValidarConfirmacionFactura(int facturaId)
        {
            return Ok(await _service.ValidarConfirmacionAsync(facturaId));
        }

        [HttpPost("facturas/{facturaId}/confirmar")]
        public async Task<IActionResult> ConfirmarFactura(int facturaId)
        {
            try
            {
                return Ok(await _service.ConfirmarVentaAsync(facturaId));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

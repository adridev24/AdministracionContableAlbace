using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/acuerdos")]
    public class AcuerdosController : ControllerBase
    {
        private readonly IComercialService _service;

        public AcuerdosController(IComercialService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAcuerdoRequest request)
        {
            try
            {
                var resultado = await _service.CreateAcuerdoAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resultado = await _service.GetAcuerdoDetalleAsync(id);
            if (resultado == null)
            {
                return NotFound();
            }
            return Ok(resultado);
        }

        [HttpGet("cliente/{clienteExternoId}")]
        public async Task<IActionResult> GetByCliente(string clienteExternoId)
        {
            return Ok(await _service.GetAcuerdosPorClienteAsync(clienteExternoId));
        }

        [HttpGet("obra/{obraExternaId}")]
        public async Task<IActionResult> GetByObra(string obraExternaId)
        {
            return Ok(await _service.GetAcuerdosPorObraAsync(obraExternaId));
        }

        [HttpPost("{id}/vias")]
        public async Task<IActionResult> CreateVia(int id, [FromBody] CreateAcuerdoViaRequest request)
        {
            try
            {
                var resultado = await _service.CrearViaAsync(id, request);
                return CreatedAtAction(nameof(GetById), new { id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/plan-pago")]
        public async Task<IActionResult> CreatePlanPago(int id, [FromBody] CreatePlanPagoRequest request)
        {
            try
            {
                var acuerdo = await _service.GetAcuerdoDetalleAsync(id);
                if (acuerdo == null)
                {
                    return NotFound();
                }
                if (acuerdo.Vias.Count != 1)
                {
                    return BadRequest(new { error = "Debe crear el plan desde una vía específica." });
                }
                var resultado = await _service.CrearPlanPagoAsync(acuerdo.Vias[0].Id, request);
                return CreatedAtAction(nameof(GetById), new { id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/api/comercial/acuerdos-vias/{id}/plan-pago")]
        public async Task<IActionResult> CreatePlanPagoVia(int id, [FromBody] CreatePlanPagoRequest request)
        {
            try
            {
                var resultado = await _service.CrearPlanPagoAsync(id, request);
                return CreatedAtAction(nameof(GetById), new { id = resultado.AcuerdoComercialId }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/plan-pago")]
        public async Task<IActionResult> UpdatePlanPago(int id, [FromBody] UpdatePlanPagoRequest request)
        {
            try
            {
                var acuerdo = await _service.GetAcuerdoDetalleAsync(id);
                if (acuerdo == null)
                {
                    return NotFound();
                }
                if (acuerdo.Vias.Count != 1)
                {
                    return BadRequest(new { error = "Debe actualizar el plan desde una vía específica." });
                }
                var resultado = await _service.ActualizarPlanPagoAsync(acuerdo.Vias[0].Id, request);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("/api/comercial/acuerdos-vias/{id}/plan-pago")]
        public async Task<IActionResult> UpdatePlanPagoVia(int id, [FromBody] UpdatePlanPagoRequest request)
        {
            try
            {
                var resultado = await _service.ActualizarPlanPagoAsync(id, request);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("/api/comercial/acuerdos-vias/{id}/modificar-monto")]
        public async Task<IActionResult> ModificarMontoVia(int id, [FromBody] ModificarMontoViaRequest request)
        {
            try
            {
                return Ok(await _service.ModificarMontoViaAsync(id, request));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/aprobar")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var resultado = await _service.AprobarAcuerdoAsync(id);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/estado-comercial")]
        public async Task<IActionResult> GetEstadoComercial(int id)
        {
            try
            {
                return Ok(await _service.GetEstadoComercialAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("/api/comercial/acuerdos-vias/{id}/estado-comercial")]
        public async Task<IActionResult> GetEstadoComercialVia(int id)
        {
            try
            {
                return Ok(await _service.GetEstadoComercialViaAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("/api/comercial/acuerdos-vias/{id}/hitos")]
        public async Task<IActionResult> CreateHito(int id, [FromBody] CreateHitoComercialRequest request)
        {
            try
            {
                var resultado = await _service.CrearHitoAsync(id, request);
                return CreatedAtAction(nameof(GetHitosVia), new { id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/api/comercial/acuerdos-vias/{id}/hitos")]
        public async Task<IActionResult> GetHitosVia(int id)
        {
            return Ok(await _service.GetHitosPorViaAsync(id));
        }
    }
}

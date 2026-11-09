using System.Text;
using BudgetControl.Api.Models.Fiscal;
using BudgetControl.Api.Services.Fiscal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Controllers;

[ApiController, Authorize, Route("api/reportes-fiscales")]
public class ReportesFiscalesController(ReportesFiscalesService service) : ControllerBase
{
    private async Task<IActionResult> Ejecutar(Func<Task<IActionResult>> accion)
    {
        try { return await accion(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { error = "Los datos cambiaron. Vuelva a consultar." }); }
        catch (DbUpdateException) { return Conflict(new { error = "No se pudo guardar. Vuelva a consultar para comprobar si otro usuario actualizó los datos." }); }
    }
    [HttpGet("empresa")] public Task<IActionResult> Empresa() => Ejecutar(async () => Ok(await service.Empresa()));
    [HttpPut("empresa")] public Task<IActionResult> Empresa(EmpresaFiscal datos) => Ejecutar(async () => Ok(await service.GuardarEmpresa(datos)));
    [HttpGet("catalogos")] public IActionResult Catalogos() => Ok(new { tipos = IvaSimpleFormato.Tipos.Select(x => new { codigo = x.Key, descripcion = x.Value }) });
    [HttpGet("periodo")] public Task<IActionResult> Periodo(string periodo) => Ejecutar(async () => Ok(await service.Consultar(periodo)));
    [HttpPut("ventas/{id:int}/fiscal")] public Task<IActionResult> Venta(int id, VentaFiscal datos) => Ejecutar(async () => { await service.GuardarVenta(id,datos); return NoContent(); });
    [HttpPut("retenciones/{id:int}/fiscal")] public Task<IActionResult> Retencion(int id, RetencionFiscal datos) => Ejecutar(async () => { await service.GuardarRetencion(id,datos); return NoContent(); });
    [HttpPost("exportaciones")] public Task<IActionResult> Exportar([FromQuery] string periodo) => Ejecutar(async () => {
        var e = await service.Exportar(periodo); return File(e.Contenido,"application/zip",e.NombreArchivo);
    });
    [HttpGet("exportaciones")] public Task<IActionResult> Historial(string periodo) => Ejecutar(async () => Ok(await service.Historial(periodo)));
    [HttpGet("exportaciones/{id:long}")] public Task<IActionResult> Descargar(long id) => Ejecutar(async () => {
        var e = await service.Descargar(id); return e == null ? NotFound() : File(e.Contenido,"application/zip",e.NombreArchivo);
    });
    [HttpGet("libro.csv")] public Task<IActionResult> LibroCsv(string periodo) => Ejecutar(async () => {
        var r = await service.Consultar(periodo);
        var filas = new List<IEnumerable<object?>> { new object?[] {"Empresa",r.Empresa?.RazonSocial,"CUIT",r.Empresa?.Cuit,"Período",periodo},
            new object?[] {"ID","Fecha","Tipo ARCA","Punto venta","Número","Cliente","Documento","Moneda","Cotización","Neto","IVA","Exento","No gravado","IIBB","Total","Neto ARS","IVA ARS","Total ARS","Alícuotas","Datos fiscales confirmados"} };
        filas.AddRange(r.Ventas.Select(v => new object?[] {v.Id,v.Fecha.ToString("yyyy-MM-dd"),v.Fiscal.TipoComprobanteArca,v.PuntoVenta,v.Numero,v.Fiscal.RazonSocial,v.Fiscal.Documento,
            v.Moneda,v.Cotizacion,v.Signo*v.Neto,v.Signo*v.Iva,v.Signo*v.Exento,v.Signo*v.NoGravado,v.Signo*v.Percepciones,v.Signo*v.Total,
            IvaSimpleFormato.Redondear(v.Signo*v.Neto*v.Cotizacion),IvaSimpleFormato.Redondear(v.Signo*v.Iva*v.Cotizacion),IvaSimpleFormato.Redondear(v.Signo*v.Total*v.Cotizacion),
            string.Join(" | ",v.Alicuotas.Select(a => $"{a.Porcentaje}%: neto {a.Neto*v.Signo}; IVA {a.Iva*v.Signo}")),v.DatosConfirmados ? "Sí" : "No"}));
        return Csv(ConciliacionFiscalService.Csv(filas),$"Libro_IVA_Ventas_{periodo}.csv");
    });
    [HttpGet("retenciones.csv")] public Task<IActionResult> RetencionesCsv(string periodo) => Ejecutar(async () => {
        var r = await service.Consultar(periodo);
        var filas = new List<IEnumerable<object?>> { new object?[] {"Empresa",r.Empresa?.RazonSocial,"CUIT",r.Empresa?.Cuit,"Período",periodo},
            new object?[] {"Cobranza","ID retención","Impuesto","Fecha cobranza","CUIT agente","Agente","Certificado","Régimen","Jurisdicción","Fecha certificado","Período cómputo","Moneda","Importe","Cotización","Importe ARS","Computable en período"} };
        filas.AddRange(r.Retenciones.Select(v => new object?[] {v.CobranzaId,v.Id,v.Impuesto,v.FechaCobranza.ToString("yyyy-MM-dd"),v.Fiscal?.CuitAgente,v.Fiscal?.RazonSocialAgente,v.Fiscal?.Certificado,
            v.Fiscal?.Regimen,v.Fiscal?.Jurisdiccion,v.Fiscal?.FechaRetencion.ToString("yyyy-MM-dd"),v.Fiscal?.PeriodoComputo,v.Moneda,v.Importe,v.Cotizacion,
            IvaSimpleFormato.Redondear(v.Importe*v.Cotizacion),v.Fiscal?.PeriodoComputo == periodo ? "Sí" : "No"}));
        return Csv(ConciliacionFiscalService.Csv(filas),$"Retenciones_sufridas_{periodo}.csv");
    });
    [HttpGet("plantilla-retenciones.csv")] public IActionResult Plantilla() => Csv(ConciliacionFiscalService.CabeceraRetenciones+"\r\n","Plantilla_conciliacion_retenciones.csv");
    [HttpPost("conciliacion/ventas"), RequestSizeLimit(22000000)]
    public Task<IActionResult> ConciliarVentas([FromForm] string periodo, [FromForm] string cuitEmpresa, IFormFile comprobantes, IFormFile alicuotas) => Ejecutar(async () => {
        await VerificarEmpresa(cuitEmpresa);
        var r = await service.Consultar(periodo);
        return Ok(ConciliacionFiscalService.Ventas(r,await Leer(comprobantes,IvaSimpleFormato.Ansi),await Leer(alicuotas,IvaSimpleFormato.Ansi)));
    });
    [HttpPost("conciliacion/retenciones"), RequestSizeLimit(11000000)]
    public Task<IActionResult> ConciliarRetenciones([FromForm] string periodo, [FromForm] string cuitEmpresa, IFormFile archivo) => Ejecutar(async () => {
        await VerificarEmpresa(cuitEmpresa);
        return Ok(ConciliacionFiscalService.Retenciones(await service.Consultar(periodo),await Leer(archivo,Encoding.UTF8)));
    });
    private async Task VerificarEmpresa(string cuit)
    {
        var empresa = await service.Empresa();
        if (empresa == null || empresa.Cuit != IvaSimpleFormato.Documento(cuit)) throw new InvalidOperationException("Confirme el CUIT de la empresa a la que pertenecen los archivos de referencia.");
    }
    private static async Task<string> Leer(IFormFile archivo, Encoding encoding)
    {
        if (archivo == null || archivo.Length > 10*1024*1024) throw new InvalidOperationException("Seleccione un archivo de hasta 10 MB.");
        using var reader = new StreamReader(archivo.OpenReadStream(),encoding,true);
        return await reader.ReadToEndAsync();
    }
    private FileContentResult Csv(string texto,string nombre) => File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto)).ToArray(),"text/csv; charset=utf-8",nombre);
}

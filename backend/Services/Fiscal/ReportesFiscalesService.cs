using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Fiscal;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models.Fiscal;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Collections;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Fiscal;

public class ReportesFiscalesService(AppDbContext db, IUserContext usuario)
{
    public static (DateTime Desde, DateTime Hasta) Periodo(string periodo)
    {
        if (!DateTime.TryParseExact(periodo, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            || fecha.Year < 2000 || fecha.Year > 9998) throw new InvalidOperationException("Indique un período válido (AAAA-MM). ");
        return (DateTime.SpecifyKind(fecha, DateTimeKind.Utc), DateTime.SpecifyKind(fecha.AddMonths(1), DateTimeKind.Utc));
    }
    public Task<EmpresaFiscal?> Empresa() => db.Set<EmpresaFiscal>().AsNoTracking().SingleOrDefaultAsync();
    public async Task<EmpresaFiscal> GuardarEmpresa(EmpresaFiscal datos)
    {
        datos.Id = 1;
        datos.Cuit = IvaSimpleFormato.Documento(datos.Cuit);
        datos.RazonSocial = datos.RazonSocial.Trim();
        datos.DomicilioFiscal = datos.DomicilioFiscal.Trim();
        if (!IvaSimpleFormato.CuitValido(datos.Cuit)) throw new InvalidOperationException("CUIT de la empresa inválido.");
        if (datos.RazonSocial.Length == 0 || datos.DomicilioFiscal.Length == 0) throw new InvalidOperationException("Complete razón social y domicilio fiscal.");
        if (datos.CondicionIva is not ("Responsable inscripto" or "Exento" or "Monotributista")) throw new InvalidOperationException("Condición de IVA inválida.");
        datos.InicioActividades = FechaUtc(datos.InicioActividades);
        var actual = await db.Set<EmpresaFiscal>().FindAsync(1);
        if (actual != null && actual.Cuit != datos.Cuit && await db.Set<ExportacionFiscal>().AnyAsync())
            throw new InvalidOperationException("La empresa ya tiene exportaciones. No se puede reemplazar su CUIT.");
        RevisarVersion(actual?.ActualizadoEl, datos.ActualizadoEl);
        datos.ActualizadoEl = Ahora(); datos.ActualizadoPor = usuario.UserName;
        if (actual == null) db.Add(datos); else db.Entry(actual).CurrentValues.SetValues(datos);
        await db.SaveChangesAsync();
        return datos;
    }
    public async Task GuardarVenta(int id, VentaFiscal datos)
    {
        var venta = await db.Ventas.AsNoTracking().Include(x => x.TipoComprobante).SingleOrDefaultAsync(x => x.Id == id && x.Estado == VentaEstado.Confirmada)
            ?? throw new InvalidOperationException("No existe un comprobante confirmado con ese identificador.");
        datos.VentaId = id;
        datos.Documento = IvaSimpleFormato.Documento(datos.Documento);
        datos.RazonSocial = datos.RazonSocial.Trim();
        datos.CodigoOperacion = datos.CodigoOperacion.Trim().ToUpperInvariant();
        if (!IvaSimpleFormato.DocumentoValido(datos.TipoDocumento, datos.Documento) || datos.RazonSocial.Length == 0 || string.IsNullOrWhiteSpace(datos.CondicionIva))
            throw new InvalidOperationException("Complete documento válido, razón social y condición de IVA.");
        if (!IvaSimpleFormato.Tipos.ContainsKey(datos.TipoComprobanteArca) || IvaSimpleFormato.Signo(datos.TipoComprobanteArca) != venta.TipoComprobante.Signo)
            throw new InvalidOperationException("Tipo ARCA incompatible con el comprobante.");
        var sugerido = IvaSimpleFormato.TipoSugerido(venta.TipoComprobante.Codigo);
        if (sugerido != 0 && sugerido != datos.TipoComprobanteArca)
            throw new InvalidOperationException("El tipo ARCA debe coincidir con el tipo de la venta.");
        if (sugerido == 0 && venta.TipoComprobante.Codigo is not ("NOTA_CREDITO" or "NOTA_DEBITO"))
            throw new InvalidOperationException("Este tipo de venta todavía no tiene una equivalencia ARCA verificada.");
        if (sugerido == 0 && !(venta.TipoComprobante.Codigo == "NOTA_CREDITO" ? new[] {3,8,21}.Contains(datos.TipoComprobanteArca) : new[] {2,7,20}.Contains(datos.TipoComprobanteArca)))
            throw new InvalidOperationException("Seleccione la letra de la nota de crédito/débito original.");
        if (datos.CodigoOperacion is not ("" or "E" or "N" or "A" or "X" or "Z")) throw new InvalidOperationException("Código de operación no admitido.");
        var actual = await db.Set<VentaFiscal>().FindAsync(id);
        RevisarVersion(actual?.ActualizadoEl, datos.ActualizadoEl);
        datos.ActualizadoEl = Ahora(); datos.ActualizadoPor = usuario.UserName;
        if (actual == null) db.Add(datos); else db.Entry(actual).CurrentValues.SetValues(datos);
        await db.SaveChangesAsync();
    }
    public async Task GuardarRetencion(int id, RetencionFiscal datos)
    {
        var medio = await db.CobranzasMediosPago.AsNoTracking().Include(x => x.MedioPago).Include(x => x.Cobranza)
            .SingleOrDefaultAsync(x => x.Id == id) ?? throw new InvalidOperationException("Retención inexistente.");
        if (!medio.MedioPago.Codigo.StartsWith("RETENCION_", StringComparison.Ordinal) || medio.Cobranza.Estado != CobranzaEstado.Confirmada)
            throw new InvalidOperationException("Solo se complementan retenciones de cobranzas confirmadas.");
        datos.CobranzaMedioPagoId = id;
        datos.CuitAgente = IvaSimpleFormato.Documento(datos.CuitAgente);
        datos.Certificado = datos.Certificado.Trim(); datos.Regimen = datos.Regimen.Trim();
        datos.RazonSocialAgente = datos.RazonSocialAgente.Trim();
        if (!IvaSimpleFormato.CuitValido(datos.CuitAgente) || datos.Certificado.Length == 0 || datos.Regimen.Length == 0 || datos.RazonSocialAgente.Length == 0)
            throw new InvalidOperationException("Complete CUIT del agente, razón social, régimen y certificado.");
        Periodo(datos.PeriodoComputo);
        if (datos.FechaRetencion.Year < 2000) throw new InvalidOperationException("Indique la fecha del certificado.");
        datos.FechaRetencion = FechaUtc(datos.FechaRetencion)!.Value;
        if (medio.MedioPago.Codigo == "RETENCION_IIBB" && string.IsNullOrWhiteSpace(datos.Jurisdiccion)) throw new InvalidOperationException("Indique la jurisdicción de IIBB.");
        var actual = await db.Set<RetencionFiscal>().FindAsync(id);
        RevisarVersion(actual?.ActualizadoEl, datos.ActualizadoEl);
        datos.ActualizadoEl = Ahora(); datos.ActualizadoPor = usuario.UserName;
        if (actual == null) db.Add(datos); else db.Entry(actual).CurrentValues.SetValues(datos);
        await db.SaveChangesAsync();
    }
    private static DateTime? FechaUtc(DateTime? fecha) => fecha.HasValue ? DateTime.SpecifyKind(fecha.Value.Date, DateTimeKind.Utc) : null;
    // PostgreSQL conserva microsegundos; el token devuelto debe coincidir con el persistido.
    private static DateTime Ahora() { var now = DateTime.UtcNow; return new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Utc); }
    private static void RevisarVersion(DateTime? actual, DateTime enviada)
    {
        if (actual.HasValue && actual.Value != enviada) throw new InvalidOperationException("Los datos fueron modificados. Vuelva a consultar antes de guardar.");
    }
    public async Task<ReporteFiscal> Consultar(string periodo)
    {
        var (desde,hasta) = Periodo(periodo);
        var reporte = new ReporteFiscal { Periodo = periodo, Empresa = await Empresa() };
        var ventas = await db.Ventas.AsNoTracking().Include(x => x.TipoComprobante).Include(x => x.Detalles).Include(x => x.PercepcionesIibb)
            .Where(x => x.FechaComprobante >= desde && x.FechaComprobante < hasta).OrderBy(x => x.FechaComprobante).ThenBy(x => x.Id).ToListAsync();
        var ids = ventas.Select(x => x.Id).ToList();
        var complementos = await db.Set<VentaFiscal>().AsNoTracking().Where(x => ids.Contains(x.VentaId)).ToDictionaryAsync(x => x.VentaId);
        var clientes = ventas.Select(x => x.ClienteExternoId).Distinct().ToList();
        var referencias = await db.ClientesReferencia.AsNoTracking().Where(x => clientes.Contains(x.ClienteExternoId)).ToDictionaryAsync(x => x.ClienteExternoId);
        if (reporte.Empresa == null) reporte.Observaciones.Add(new("Error","Empresa",null,"Configure los datos fiscales de la empresa."));
        else if (reporte.Empresa.CondicionIva != "Responsable inscripto") reporte.Observaciones.Add(new("Error","Empresa",null,"IVA Simple requiere que la empresa sea responsable inscripta."));
        foreach (var venta in ventas)
        {
            if (venta.Estado != VentaEstado.Confirmada)
            {
                reporte.Observaciones.Add(new("Advertencia","Venta",venta.Id,$"Comprobante {venta.Estado}: excluido del libro. Verifique su situación frente a ARCA."));
                continue;
            }
            var fila = Proyectar(venta);
            if (complementos.TryGetValue(venta.Id, out var fiscal)) { fila.Fiscal = fiscal; fila.DatosConfirmados = true; }
            else if (referencias.TryGetValue(venta.ClienteExternoId, out var cliente))
            {
                fila.Fiscal.RazonSocial = cliente.Nombre;
                fila.Fiscal.Documento = IvaSimpleFormato.Documento(cliente.Documento);
            }
            reporte.Ventas.Add(fila);
            foreach (var error in IvaSimpleFormato.Validar(fila)) reporte.Observaciones.Add(new("Error","Venta",venta.Id,error));
            if (venta.PercepcionIibbRequiereRecalculo || Math.Abs(venta.PercepcionesIibb.Where(x => x.Activa).Sum(x => x.Importe)-venta.TotalPercepciones) > 0.01m)
                reporte.Observaciones.Add(new("Error","Venta",venta.Id,"Percepciones pendientes de recálculo o inconsistentes con el detalle."));
            if (!venta.AsientoContableId.HasValue) reporte.Observaciones.Add(new("Advertencia","Venta",venta.Id,"Comprobante sin asiento contable vinculado."));
        }
        foreach (var repetido in reporte.Ventas.GroupBy(x => x.Clave).Where(g => g.Count() > 1))
            foreach (var fila in repetido) reporte.Observaciones.Add(new("Error","Venta",fila.Id,$"Comprobante duplicado: {fila.Clave}."));
        foreach (var serie in reporte.Ventas.GroupBy(x => new {x.Fiscal.TipoComprobanteArca,x.PuntoVenta}))
        {
            var numeros = serie.Select(x => x.Numero).Distinct().Order().ToArray();
            for (var i = 1; i < numeros.Length; i++)
                if (numeros[i] - numeros[i-1] > 1) reporte.Observaciones.Add(new("Advertencia","Numeracion",null,$"Tipo {serie.Key.TipoComprobanteArca}, PV {serie.Key.PuntoVenta}: salto entre {numeros[i-1]} y {numeros[i]}."));
        }
        // Incluye el mes de cobro y las retenciones asignadas al período de cómputo.
        var datosRetenciones = db.Set<RetencionFiscal>().AsNoTracking();
        var medios = await db.CobranzasMediosPago.AsNoTracking().Include(x => x.Cobranza).Include(x => x.MedioPago)
            .Where(x => x.Cobranza.Estado == CobranzaEstado.Confirmada && x.MedioPago.Codigo.StartsWith("RETENCION_")
                && ((x.Cobranza.Fecha >= desde && x.Cobranza.Fecha < hasta) || datosRetenciones.Any(r => r.CobranzaMedioPagoId == x.Id && r.PeriodoComputo == periodo)))
            .OrderBy(x => x.Cobranza.Fecha).ThenBy(x => x.Id).ToListAsync();
        var medioIds = medios.Select(x => x.Id).ToList();
        var retenciones = await datosRetenciones.Where(x => medioIds.Contains(x.CobranzaMedioPagoId)).ToDictionaryAsync(x => x.CobranzaMedioPagoId);
        foreach (var medio in medios)
        {
            retenciones.TryGetValue(medio.Id, out var fiscal);
            reporte.Retenciones.Add(new(medio.Id, medio.CobranzaId, medio.Cobranza.ClienteExternoId, medio.Cobranza.Fecha,
                medio.MedioPago.Codigo.Replace("RETENCION_", ""), medio.Cobranza.MonedaCodigo, medio.Cobranza.Cotizacion, medio.Importe, fiscal));
            if (fiscal == null) reporte.Observaciones.Add(new("Error","Retencion",medio.Id,"Faltan los datos del certificado. El importe proviene de la cobranza."));
            else if (fiscal.PeriodoComputo != periodo) reporte.Observaciones.Add(new("Advertencia","Retencion",medio.Id,$"Computable en {fiscal.PeriodoComputo}; no integrar al total computable de {periodo}."));
            if (medio.Cobranza.Cotizacion <= 0 || (IvaSimpleFormato.Moneda(medio.Cobranza.MonedaCodigo) == "PES" && medio.Cobranza.Cotizacion != 1))
                reporte.Observaciones.Add(new("Error","Retencion",medio.Id,"Cotización de la cobranza inválida."));
        }
        // Duplicados globales, incluso si los certificados se imputaron en meses diferentes.
        var certificados = await (from r in datosRetenciones join m in db.CobranzasMediosPago on r.CobranzaMedioPagoId equals m.Id
            where m.Cobranza.Estado == CobranzaEstado.Confirmada
            select new { r.CobranzaMedioPagoId, r.CuitAgente, r.Certificado, r.Regimen, r.Jurisdiccion, m.MedioPago.Codigo }).ToListAsync();
        foreach (var duplicado in certificados.GroupBy(x => new {x.CuitAgente,x.Certificado,x.Regimen,x.Jurisdiccion,x.Codigo}).Where(g => g.Count() > 1))
            foreach (var item in duplicado.Where(x => medioIds.Contains(x.CobranzaMedioPagoId)))
                reporte.Observaciones.Add(new("Error","Retencion",item.CobranzaMedioPagoId,"Certificado duplicado en cobranzas confirmadas."));
        return reporte;
    }
    public static VentaLibroFiscal Proyectar(Venta venta)
    {
        FiscalRegistrado? simple = null;
        if (venta.FiscalSimpleJson != null)
        {
            try { simple = JsonSerializer.Deserialize<FiscalRegistrado>(venta.FiscalSimpleJson); }
            catch (JsonException) { throw new InvalidOperationException($"La información fiscal de la venta {venta.Id} no se puede leer."); }
        }
        var lineas = simple != null ? simple.Lineas.Select(x => new AlicuotaFiscal(x.Porcentaje,x.Neto,x.Iva))
            : venta.Detalles.Where(x => x.TipoTratamientoIva == TipoTratamientoIvaVenta.Gravado).Select(x => new AlicuotaFiscal(x.PorcentajeIvaAplicado,x.Neto,x.ImporteIva));
        return new VentaLibroFiscal {
            Id = venta.Id, Fecha = venta.FechaComprobante, ClienteId = venta.ClienteExternoId, Tipo = venta.TipoComprobante.Descripcion,
            PuntoVenta = venta.PuntoVenta, Numero = venta.NumeroComprobante, Signo = venta.TipoComprobante.Signo,
            Moneda = venta.MonedaCodigo, Cotizacion = venta.Cotizacion, Neto = venta.NetoGravado, Iva = venta.TotalIva,
            Exento = venta.TotalExento, NoGravado = venta.TotalNoGravado, Percepciones = venta.TotalPercepciones, Total = venta.Total,
            DevolucionCheques = simple?.DevolucionCheques ?? 0,
            Alicuotas = lineas.GroupBy(x => x.Porcentaje).OrderBy(x => x.Key).Select(g => new AlicuotaFiscal(g.Key,g.Sum(x => x.Neto),g.Sum(x => x.Iva))).ToList(),
            Fiscal = new VentaFiscal { VentaId = venta.Id, TipoComprobanteArca = IvaSimpleFormato.TipoSugerido(venta.TipoComprobante.Codigo),
                CodigoOperacion = venta.TipoComprobante.EsExportacion ? "X" : venta.TotalIva != 0 ? "" : venta.TotalExento > 0 && venta.TotalNoGravado == 0 && venta.NetoGravado == 0 ? "E" : "" }
        };
    }
    public async Task<ExportacionFiscal> Exportar(string periodo)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var reporte = await Consultar(periodo);
        if (!reporte.PuedeExportar) throw new InvalidOperationException("La exportación requiere ventas y la resolución de todos los errores de empresa y ventas.");
        var cabeceras = new List<string>(); var alicuotas = new List<string>();
        foreach (var venta in reporte.Ventas) { var r = IvaSimpleFormato.Registros(venta); cabeceras.Add(r.Cabecera); alicuotas.AddRange(r.Alicuotas); }
        var cbte = IvaSimpleFormato.Ansi.GetBytes(string.Join("\r\n",cabeceras)+"\r\n");
        var tasas = IvaSimpleFormato.Ansi.GetBytes(string.Join("\r\n",alicuotas)+"\r\n");
        if (cbte.Length > 10*1024*1024 || tasas.Length > 10*1024*1024) throw new InvalidOperationException("El período excede el límite de 10 MB por TXT; requiere exportación por lotes.");
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            Agregar(zip,"LIBRO_IVA_DIGITAL_VENTAS_CBTE.txt",cbte);
            Agregar(zip,"LIBRO_IVA_DIGITAL_VENTAS_ALICUOTAS.txt",tasas);
            Agregar(zip,"respaldo.json",JsonSerializer.SerializeToUtf8Bytes(new { version = IvaSimpleFormato.Version, reporte }));
            Agregar(zip,"LEER.txt",Encoding.UTF8.GetBytes($"Empresa: {reporte.Empresa!.RazonSocial}\r\nCUIT: {reporte.Empresa.Cuit}\r\nPeríodo: {periodo}\r\nSolo ventas. No constituye una declaración jurada ni incluye compras.\r\nExtraer el ZIP y cargar ambos TXT en Portal IVA, indicando IMPORTES EN MONEDA ORIGINAL DEL COMPROBANTE.\r\nNotas de crédito: importes positivos en TXT; signo según tipo.\r\nFormato: {IvaSimpleFormato.Version}. ANSI Windows-1252, CRLF.\r\nLa exportación no acredita presentación ni aceptación por ARCA.\r\n"));
        }
        var bytes = buffer.ToArray();
        var exportacion = new ExportacionFiscal { Periodo = periodo, GeneradoEl = DateTime.UtcNow, GeneradoPor = usuario.UserName,
            NombreArchivo = $"IVA_Ventas_{reporte.Empresa!.Cuit}_{periodo}.zip", Contenido = bytes,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)), CantidadComprobantes = reporte.Ventas.Count };
        db.Add(exportacion); await db.SaveChangesAsync(); await tx.CommitAsync();
        return exportacion;
    }
    private static void Agregar(ZipArchive zip, string nombre, byte[] bytes) { using var archivo = zip.CreateEntry(nombre).Open(); archivo.Write(bytes); }
    public async Task<object> Historial(string periodo)
    {
        Periodo(periodo);
        return await db.Set<ExportacionFiscal>().AsNoTracking().Where(x => x.Periodo == periodo).OrderByDescending(x => x.Id)
            .Select(x => new {x.Id,x.Periodo,x.GeneradoEl,x.GeneradoPor,x.NombreArchivo,x.Sha256,x.CantidadComprobantes}).ToListAsync();
    }
    public Task<ExportacionFiscal?> Descargar(long id) => db.Set<ExportacionFiscal>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
}

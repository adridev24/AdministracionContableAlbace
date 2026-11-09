using System.Globalization;
using System.Text;
using BudgetControl.Api.DTOs.Fiscal;
using Microsoft.VisualBasic.FileIO;

namespace BudgetControl.Api.Services.Fiscal;

public static class ConciliacionFiscalService
{
    private static List<string> LeerLineas(string texto, int ancho)
    {
        var lineas = texto.TrimStart('\uFEFF').Replace("\r\n","\n").Split('\n').ToList();
        while (lineas.Count > 0 && lineas[^1] == "") lineas.RemoveAt(lineas.Count-1);
        if (lineas.Any(x => x.Length != ancho)) throw new InvalidOperationException($"El archivo debe tener {ancho} caracteres por registro, sin encabezados ni líneas vacías intermedias.");
        return lineas;
    }
    private static decimal Num(string linea, int inicio, int largo, decimal divisor = 1)
    {
        var texto = linea.Substring(inicio,largo);
        if (!texto.All(char.IsAsciiDigit) || !decimal.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var numero))
            throw new InvalidOperationException("El archivo contiene un campo numérico inválido.");
        return numero/divisor;
    }
    private static string Clave(string linea, int inicio) => $"{Num(linea,inicio,3):000}-{Num(linea,inicio+3,5):00000}-{Num(linea,inicio+8,20):00000000000000000000}";
    public static ConciliacionFiscal Ventas(ReporteFiscal reporte, string cabecera, string alicuotas)
    {
        var (desde,hasta) = ReportesFiscalesService.Periodo(reporte.Periodo);
        var cabeceras = LeerLineas(cabecera,266);
        var tasas = LeerLineas(alicuotas,62);
        foreach (var c in cabeceras)
        {
            if (!DateTime.TryParseExact(c[..8],"yyyyMMdd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var fecha) || fecha < desde || fecha >= hasta)
                throw new InvalidOperationException("El archivo contiene fechas inválidas o comprobantes fuera del período seleccionado.");
            if (Num(c,16,20) != Num(c,36,20)) throw new InvalidOperationException("Esta conciliación requiere comprobantes individuales, no rangos diarios agrupados.");
            foreach (var inicio in new[] {108,123,138,153,168,183,198,213,243}) Num(c,inicio,15);
            Num(c,56,2); Num(c,58,20); Num(c,231,10); Num(c,241,1);
        }
        foreach (var t in tasas) { Clave(t,0); Num(t,28,15); Num(t,43,4); Num(t,47,15); }
        var referencias = cabeceras.GroupBy(x => Clave(x,8)).ToDictionary(g => g.Key,g => g.ToList());
        var detalles = tasas.GroupBy(x => Clave(x,0)).ToDictionary(g => g.Key,g => g.ToList());
        var locales = reporte.Ventas.GroupBy(x => x.Clave).ToDictionary(g => g.Key,g => g.ToList());
        var diferencias = new List<DiferenciaFiscal>(); var coincidentes = 0;
        foreach (var key in referencias.Keys.Union(locales.Keys).Union(detalles.Keys).Order())
        {
            referencias.TryGetValue(key,out var refs); locales.TryGetValue(key,out var filas); detalles.TryGetValue(key,out var dt);
            if ((refs?.Count ?? 0) > 1 || (filas?.Count ?? 0) > 1) { diferencias.Add(new(key,"Duplicado","Clave repetida en sistema o referencia.")); continue; }
            if (refs == null) { diferencias.Add(new(key,dt != null ? "Detalle sin cabecera" : "Solo sistema","No figura en el archivo de comprobantes de referencia.")); continue; }
            if (filas == null) { diferencias.Add(new(key,"Solo referencia","No figura entre las ventas confirmadas del período.")); continue; }
            var v = filas[0]; var c = refs[0]; var errores = new List<string>();
            if (c[..8] != v.Fecha.ToString("yyyyMMdd",CultureInfo.InvariantCulture)) errores.Add("fecha");
            if (Num(c,56,2) != v.Fiscal.TipoDocumento || c.Substring(58,20).TrimStart('0') != v.Fiscal.Documento.TrimStart('0')) errores.Add("documento comprador");
            if (c.Substring(228,3) != IvaSimpleFormato.Moneda(v.Moneda) || Num(c,231,10,1000000) != v.Cotizacion) errores.Add("moneda/cotización (usar moneda original)");
            foreach (var dato in new[] { (108,v.Total,"total"),(123,v.NoGravado,"no gravado"),(153,v.Exento,"exento"),(183,v.Percepciones,"IIBB") })
                if (Math.Abs(Num(c,dato.Item1,15,100)-dato.Item2) > 0.01m) errores.Add(dato.Item3);
            if (new[] {138,168,198,213,243}.Any(i => Num(c,i,15) != 0)) errores.Add("otros tributos no registrados en la venta");
            if (c[242].ToString().Trim().Replace("0","") != v.Fiscal.CodigoOperacion) errores.Add("código de operación");
            if (Num(c,241,1) != (dt?.Count ?? 0)) errores.Add("cantidad de alícuotas de referencia");
            var esperadas = IvaSimpleFormato.Lineas(v).OrderBy(x => x.Porcentaje).ToList();
            if ((dt?.Count ?? 0) != esperadas.Count) errores.Add("cantidad de alícuotas del sistema");
            else foreach (var a in esperadas)
            {
                var similares = dt!.Where(t => Num(t,43,4) == IvaSimpleFormato.CodigoAlicuota(a.Porcentaje)).ToList();
                if (similares.Count != 1 || Math.Abs(Num(similares[0],28,15,100)-a.Neto) > 0.01m || Math.Abs(Num(similares[0],47,15,100)-a.Iva) > 0.01m) errores.Add($"neto/IVA alícuota {a.Porcentaje}%");
            }
            if (errores.Count == 0) coincidentes++; else diferencias.Add(new(key,"Diferencia",string.Join(", ",errores)));
        }
        return new(reporte.Ventas.Count,cabeceras.Count,coincidentes,diferencias);
    }
    public const string CabeceraRetenciones = "impuesto;cuit_agente;certificado;regimen;jurisdiccion;fecha;importe_ars";
    public static ConciliacionFiscal Retenciones(ReporteFiscal reporte, string csv)
    {
        using var parser = new TextFieldParser(new StringReader(csv.TrimStart('\uFEFF')));
        parser.SetDelimiters(";"); parser.HasFieldsEnclosedInQuotes = true;
        if (string.Join(";",parser.ReadFields() ?? []) != CabeceraRetenciones) throw new InvalidOperationException("Use la plantilla de conciliación de retenciones del módulo (CSV separado por punto y coma).");
        var referencias = new List<(string Clave, DateTime Fecha, decimal Importe)>();
        try
        {
            while (!parser.EndOfData)
            {
                var c = parser.ReadFields()!;
                if (c.Length != 7 || !new[] {"IVA","GANANCIAS","SUSS","IIBB"}.Contains(c[0]) || !IvaSimpleFormato.CuitValido(IvaSimpleFormato.Documento(c[1]))
                    || c[2].Length == 0 || c[3].Length == 0 || !DateTime.TryParseExact(c[5],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var fecha)
                    || !decimal.TryParse(c[6].Replace(',','.'),NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var importe) || importe <= 0)
                    throw new InvalidOperationException("Registro de retención inválido. Use fecha AAAA-MM-DD e importe positivo sin separador de miles.");
                referencias.Add((ClaveRet(c[0],IvaSimpleFormato.Documento(c[1]),c[2],c[3],c[4]),fecha,importe));
            }
        }
        catch (MalformedLineException) { throw new InvalidOperationException("CSV de retenciones mal formado."); }
        var locales = reporte.Retenciones.Where(x => x.Fiscal?.PeriodoComputo == reporte.Periodo).ToList();
        var local = locales.GroupBy(x => ClaveRet(x.Impuesto,x.Fiscal!.CuitAgente,x.Fiscal.Certificado,x.Fiscal.Regimen,x.Fiscal.Jurisdiccion)).ToDictionary(g => g.Key,g => g.ToList());
        var externo = referencias.GroupBy(x => x.Clave).ToDictionary(g => g.Key,g => g.ToList());
        var diferencias = new List<DiferenciaFiscal>(); var coincidentes = 0;
        foreach (var pendiente in reporte.Retenciones.Where(x => x.Fiscal == null)) diferencias.Add(new($"Cobranza {pendiente.CobranzaId} / retención {pendiente.Id}","Sin datos fiscales","No se puede identificar el certificado ni determinar su período de cómputo."));
        foreach (var key in local.Keys.Union(externo.Keys).Order())
        {
            local.TryGetValue(key,out var l); externo.TryGetValue(key,out var e);
            if ((l?.Count ?? 0) > 1 || (e?.Count ?? 0) > 1) diferencias.Add(new(key,"Duplicado","Certificado repetido en sistema o referencia."));
            else if (l == null) diferencias.Add(new(key,"Solo referencia","Certificado no registrado para el período de cómputo."));
            else if (e == null) diferencias.Add(new(key,"Solo sistema","Certificado ausente en la referencia."));
            else if (l[0].Fiscal!.FechaRetencion.Date != e[0].Fecha.Date || Math.Abs(IvaSimpleFormato.Redondear(l[0].Importe*l[0].Cotizacion)-e[0].Importe) > 0.01m)
                diferencias.Add(new(key,"Diferencia","Fecha o importe en pesos diferente."));
            else coincidentes++;
        }
        return new(locales.Count,referencias.Count,coincidentes,diferencias);
    }
    private static string ClaveRet(params string[] campos) => JsonSerializerKey(campos);
    private static string JsonSerializerKey(string[] campos) => System.Text.Json.JsonSerializer.Serialize(campos.Select(x => x.Trim().ToUpperInvariant()));
    public static string Csv(IEnumerable<IEnumerable<object?>> filas) => string.Join("\r\n",filas.Select(f => string.Join(";",f.Select(Celda))))+"\r\n";
    private static string Celda(object? value)
    {
        var text = value is decimal d ? d.ToString("0.00####",CultureInfo.GetCultureInfo("es-AR")) : Convert.ToString(value,CultureInfo.InvariantCulture) ?? "";
        if (value is string && text.TrimStart().StartsWithAnyFormula()) text = "'"+text;
        return "\""+text.Replace("\"","\"\"")+"\"";
    }
    private static bool StartsWithAnyFormula(this string text) => text.Length > 0 && "=+-@\t\r".Contains(text[0]);
}

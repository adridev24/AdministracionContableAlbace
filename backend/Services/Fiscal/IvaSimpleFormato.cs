using System.Globalization;
using System.Text;
using BudgetControl.Api.DTOs.Fiscal;

namespace BudgetControl.Api.Services.Fiscal;

public static class IvaSimpleFormato
{
    public const string Version = "Ventas-266-Alícuotas-62/2026-11";
    public static readonly IReadOnlyDictionary<int, string> Tipos = new Dictionary<int, string> {
        [1]="Factura A", [2]="Nota de débito A", [3]="Nota de crédito A",
        [6]="Factura B", [7]="Nota de débito B", [8]="Nota de crédito B",
        [11]="Factura C", [12]="Nota de débito C", [13]="Nota de crédito C",
        [19]="Factura E", [20]="Nota de débito E", [21]="Nota de crédito E",
        [51]="Factura M", [52]="Nota de débito M", [53]="Nota de crédito M",
        [201]="FCE A", [202]="Nota de débito FCE A", [203]="Nota de crédito FCE A",
        [206]="FCE B", [207]="Nota de débito FCE B", [208]="Nota de crédito FCE B"
    };
    public static int Signo(int tipo) => tipo is 3 or 8 or 13 or 21 or 53 or 203 or 208 ? -1 : 1;
    public static int TipoSugerido(string codigo) => codigo switch {
        "FACTURA_A" => 1, "FACTURA_B" => 6, "FACTURA_C" => 11, "FACTURA_E" => 19,
        "NOTA_DEBITO_A" => 2, "NOTA_CREDITO_A" => 3, "NOTA_DEBITO_B" => 7,
        "NOTA_CREDITO_B" => 8, "NOTA_DEBITO_E" => 20, "NOTA_CREDITO_E" => 21,
        "FCE_MIPYME_A_CON_NOMENCLADOR" or "FCE_MIPYME_A_SIN_NOMENCLADOR" => 201,
        "FCE_MIPYME_NOTA_DEBITO_A_CON_NOMENCLADOR" or "FCE_MIPYME_NOTA_DEBITO_A_SIN_NOMENCLADOR" => 202,
        "FCE_MIPYME_NOTA_CREDITO_A_CON_NOMENCLADOR" or "FCE_MIPYME_NOTA_CREDITO_A_SIN_NOMENCLADOR" => 203,
        _ => 0
    };
    public static string Documento(string? value) => (value ?? "").Replace("-", "").Replace(" ", "").Trim();
    public static bool CuitValido(string value)
    {
        if (value.Length != 11 || value.Any(c => c < '0' || c > '9') || value.Distinct().Count() == 1) return false;
        int[] pesos = [5,4,3,2,7,6,5,4,3,2];
        var digito = 11 - Enumerable.Range(0, 10).Sum(i => (value[i]-'0') * pesos[i]) % 11;
        return digito != 10 && (digito == 11 ? 0 : digito) == value[10]-'0';
    }
    public static bool DocumentoValido(int tipo, string value) => tipo switch {
        80 or 86 => CuitValido(value),
        96 => value.Length is >= 7 and <= 8 && value.All(char.IsAsciiDigit) && value.Any(c => c != '0'),
        _ => false
    };
    public static string Moneda(string value) => value.ToUpperInvariant() switch {
        "ARS" or "PES" => "PES", "USD" or "DOL" => "DOL", "EUR" or "060" => "060", _ => ""
    };
    public static int CodigoAlicuota(decimal value) => value switch { 0 => 3, 10.5m => 4, 21 => 5, 27 => 6, 5 => 8, 2.5m => 9, _ => 0 };
    public static decimal Redondear(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static Encoding Ansi { get; } = CrearAnsi();
    private static Encoding CrearAnsi()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }
    public static string Numero(decimal value, int ancho, int decimales = 0)
    {
        if (value < 0) throw new InvalidOperationException("El archivo ARCA requiere importes positivos; el tipo identifica las notas de crédito.");
        var factor = decimales == 6 ? 1000000m : decimales == 2 ? 100m : 1m;
        var text = decimal.Round(value * factor, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        if (text.Length > ancho) throw new InvalidOperationException("Un número supera el ancho permitido por ARCA.");
        return text.PadLeft(ancho, '0');
    }
    public static List<AlicuotaFiscal> Lineas(VentaLibroFiscal v) => v.Alicuotas.Count > 0 ? v.Alicuotas : [new(0, 0, 0)];
    public static (string Cabecera, List<string> Alicuotas) Registros(VentaLibroFiscal v)
    {
        var f = v.Fiscal;
        var lineas = Lineas(v);
        var nombre = f.RazonSocial.Trim();
        if (nombre.Any(char.IsControl)) throw new InvalidOperationException("La razón social contiene caracteres de control.");
        Ansi.GetBytes(nombre);
        nombre = nombre.Length > 30 ? nombre[..30] : nombre.PadRight(30);
        var cabecera = v.Fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            + Numero(f.TipoComprobanteArca,3) + Numero(v.PuntoVenta,5) + Numero(v.Numero,20) + Numero(v.Numero,20)
            + Numero(f.TipoDocumento,2) + f.Documento.PadLeft(20,'0') + nombre
            + Numero(v.Total,15,2) + Numero(v.NoGravado,15,2) + Numero(0,15,2) + Numero(v.Exento,15,2)
            + Numero(0,15,2) + Numero(v.Percepciones,15,2) + Numero(0,15,2) + Numero(0,15,2)
            + Moneda(v.Moneda) + Numero(v.Cotizacion,10,6) + Numero(lineas.Count,1)
            + (string.IsNullOrEmpty(f.CodigoOperacion) ? " " : f.CodigoOperacion) + Numero(0,15,2) + "00000000";
        var alicuotas = lineas.Select(a => Numero(f.TipoComprobanteArca,3) + Numero(v.PuntoVenta,5)
            + Numero(v.Numero,20) + Numero(a.Neto,15,2) + Numero(CodigoAlicuota(a.Porcentaje),4) + Numero(a.Iva,15,2)).ToList();
        if (cabecera.Length != 266 || alicuotas.Any(x => x.Length != 62)) throw new InvalidOperationException("Longitud de registro ARCA inválida.");
        return (cabecera, alicuotas);
    }
    public static List<string> Validar(VentaLibroFiscal v)
    {
        var errores = new List<string>();
        if (!v.DatosConfirmados) errores.Add("Complete y confirme los datos fiscales del comprobante.");
        if (!Tipos.ContainsKey(v.Fiscal.TipoComprobanteArca)) errores.Add("Seleccione un tipo de comprobante ARCA admitido.");
        if (Signo(v.Fiscal.TipoComprobanteArca) != v.Signo) errores.Add("El tipo ARCA no coincide con el signo del comprobante.");
        if (!DocumentoValido(v.Fiscal.TipoDocumento, v.Fiscal.Documento)) errores.Add("Documento del comprador inválido (CUIT, CUIL o DNI).");
        if (string.IsNullOrWhiteSpace(v.Fiscal.RazonSocial) || string.IsNullOrWhiteSpace(v.Fiscal.CondicionIva)) errores.Add("Faltan razón social o condición de IVA del comprador.");
        if (v.PuntoVenta is < 1 or > 99999 || v.Numero <= 0) errores.Add("Punto de venta o número inválido.");
        if (Moneda(v.Moneda) == "" || v.Cotizacion <= 0 || v.Cotizacion > 9999.999999m || decimal.Round(v.Cotizacion,6) != v.Cotizacion) errores.Add("Moneda o cotización no admitida por el formato.");
        if (Moneda(v.Moneda) == "PES" && v.Cotizacion != 1) errores.Add("Un comprobante en pesos debe tener cotización 1.");
        if (new[] { v.Neto,v.Iva,v.Exento,v.NoGravado,v.Percepciones,v.Total }.Any(x => x < 0 || Redondear(x) != x)) errores.Add("Los importes deben ser positivos y tener hasta dos decimales.");
        if (v.Total <= 0 || Math.Abs(v.Total - (v.Neto+v.Iva+v.Exento+v.NoGravado+v.Percepciones)) > 0.01m) errores.Add("El total no coincide con la suma de los componentes fiscales.");
        if (Math.Abs(v.Alicuotas.Sum(x => x.Neto)-v.Neto) > 0.01m || Math.Abs(v.Alicuotas.Sum(x => x.Iva)-v.Iva) > 0.01m) errores.Add("El detalle por alícuota no coincide con la cabecera.");
        foreach (var a in v.Alicuotas)
        {
            if (CodigoAlicuota(a.Porcentaje) == 0 || a.Neto < 0 || a.Iva < 0) errores.Add("Alícuota o importes del detalle inválidos.");
            if (Math.Abs(Redondear(a.Neto*a.Porcentaje/100)-a.Iva) > 0.02m) errores.Add($"Revise el IVA calculado para la alícuota {a.Porcentaje}%.");
        }
        if (v.DevolucionCheques != 0 && !v.Fiscal.DevolucionChequeRevisada) errores.Add("Revise el tratamiento fiscal de la devolución de cheques incluida en no gravado.");
        if (v.Fiscal.CodigoOperacion is not ("" or "E" or "N" or "A" or "X" or "Z")) errores.Add("Código de operación no admitido.");
        if (v.Iva == 0 && v.Fiscal.CodigoOperacion == "") errores.Add("Indique el código de operación para el comprobante sin IVA.");
        if (v.Iva != 0 && v.Fiscal.CodigoOperacion != "") errores.Add("Una operación gravada debe usar el código de operación en blanco.");
        if (v.Fiscal.TipoComprobanteArca is 11 or 12 or 13) errores.Add("Los comprobantes C no corresponden al exportador de una empresa responsable inscripta.");
        if (errores.Count == 0)
        {
            try { Registros(v); }
            catch (Exception ex) when (ex is InvalidOperationException or EncoderFallbackException) { errores.Add(ex.Message); }
        }
        return errores.Distinct().ToList();
    }
}

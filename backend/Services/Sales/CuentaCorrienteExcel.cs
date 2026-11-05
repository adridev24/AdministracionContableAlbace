using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales;

public static class CuentaCorrienteExcel
{
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Crear(CuentaCorrienteClienteResponse cuenta, CuentaCorrienteClienteFilterRequest filtro)
    {
        var hojas = new (string Nombre, string[] Cabecera, IEnumerable<object?[]> Filas)[]
        {
            ("Consulta", new[] { "Dato", "Valor" }, new object?[][] {
                new object?[] { "Cliente", cuenta.ClienteNombre ?? cuenta.ClienteId },
                new object?[] { "ID cliente", cuenta.ClienteId },
                new object?[] { "Desde", Fecha(filtro.FechaDesde) }, new object?[] { "Hasta", Fecha(filtro.FechaHasta) },
                new object?[] { "Moneda", filtro.Moneda }, new object?[] { "Obra", filtro.ObraId },
                new object?[] { "Estado factura", filtro.EstadoFactura } }),
            ("Saldos", new[] { "Moneda", "Saldo anterior", "Debe período", "Haber período", "Saldo final" },
                cuenta.SaldosPorMoneda.Select(x => new object?[] { x.MonedaCodigo, x.SaldoAnterior, x.DebePeriodo, x.HaberPeriodo, x.SaldoFinal })),
            ("Movimientos", new[] { "Fecha", "Tipo", "Comprobante", "Descripción", "Obra", "Debe", "Haber", "Saldo", "Moneda" },
                cuenta.Movimientos.Select(x => new object?[] { Fecha(x.Fecha), x.TipoMovimiento, x.NumeroComprobante ?? x.IdOrigen, x.Descripcion, x.ObraNombre ?? x.ObraId, x.Debe, x.Haber, x.SaldoAcumulado, x.MonedaCodigo })),
            ("Facturas", new[] { "Fecha", "Comprobante", "Tipo", "Obra", "Moneda", "Total", "Cobrado", "Saldo", "Estado" },
                cuenta.Facturas.Select(x => new object?[] { Fecha(x.Fecha), x.NumeroComprobante, x.TipoComprobante, x.ObraNombre ?? x.ObraId, x.MonedaCodigo, x.TotalFactura, x.TotalCobrado, x.Saldo, x.EstadoCobranza })),
            ("Cobranzas aplicadas", new[] { "Factura", "Cobranza", "Fecha", "Moneda", "Importe aplicado", "Estado" },
                cuenta.Facturas.SelectMany(f => f.Cobranzas.Select(c => new object?[] { f.NumeroComprobante, c.CobranzaId, Fecha(c.Fecha), f.MonedaCodigo, c.ImporteAplicado, c.EstadoCobranza })))
        };
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            XNamespace content = "http://schemas.openxmlformats.org/package/2006/content-types";
            Escribir(zip, "[Content_Types].xml", new XElement(content + "Types",
                new XElement(content + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(content + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(content + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                hojas.Select((_, i) => new XElement(content + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{i + 1}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));
            Escribir(zip, "_rels/.rels", new XElement(Rel + "Relationships", Relacion("rId1", "officeDocument", "xl/workbook.xml")));
            Escribir(zip, "xl/workbook.xml", new XElement(Sheet + "workbook", new XElement(Sheet + "sheets",
                hojas.Select((h, i) => new XElement(Sheet + "sheet", new XAttribute("name", h.Nombre), new XAttribute("sheetId", i + 1), new XAttribute(XName.Get("id", OfficeRel), $"rId{i + 1}"))))));
            Escribir(zip, "xl/_rels/workbook.xml.rels", new XElement(Rel + "Relationships",
                hojas.Select((_, i) => Relacion($"rId{i + 1}", "worksheet", $"worksheets/sheet{i + 1}.xml"))));
            for (var i = 0; i < hojas.Length; i++)
            {
                var h = hojas[i];
                var filas = new[] { h.Cabecera.Cast<object?>().ToArray() }.Concat(h.Filas).ToList();
                Escribir(zip, $"xl/worksheets/sheet{i + 1}.xml", new XElement(Sheet + "worksheet",
                    new XElement(Sheet + "cols", new XElement(Sheet + "col", new XAttribute("min", 1), new XAttribute("max", h.Cabecera.Length), new XAttribute("width", 24), new XAttribute("customWidth", 1))),
                    new XElement(Sheet + "sheetData", filas.Select((fila, n) => new XElement(Sheet + "row", new XAttribute("r", n + 1), fila.Select(Celda))))));
            }
        }
        return output.ToArray();
    }

    private static string Fecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "";
    private static XElement Relacion(string id, string tipo, string destino) => new(Rel + "Relationship", new XAttribute("Id", id), new XAttribute("Type", OfficeRel + "/" + tipo), new XAttribute("Target", destino));
    private static XElement Celda(object? valor) => valor is decimal or int
        ? new XElement(Sheet + "c", new XElement(Sheet + "v", Convert.ToString(valor, CultureInfo.InvariantCulture)))
        : new XElement(Sheet + "c", new XAttribute("t", "inlineStr"), new XElement(Sheet + "is", new XElement(Sheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), valor?.ToString() ?? "")));
    private static void Escribir(ZipArchive zip, string ruta, XElement contenido)
    {
        using var stream = zip.CreateEntry(ruta).Open();
        contenido.Save(stream);
    }
}

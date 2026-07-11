namespace BudgetControl.Api.DTOs.Accounting
{
    public class SolicitudContabilizacionAutomaticaRequest
    {
        public string CodigoOperacion { get; set; } = string.Empty;
        public string ModuloOrigen { get; set; } = string.Empty;
        public string IdOrigen { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public Dictionary<string, decimal> ImportesPorConcepto { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ContabilizacionAutomaticaResponse
    {
        public int AsientoContableId { get; set; }
        public bool YaExistia { get; set; }
        public string CodigoOperacion { get; set; } = string.Empty;
        public string ModuloOrigen { get; set; } = string.Empty;
        public string IdOrigen { get; set; } = string.Empty;
        public string IdOrigenContable { get; set; } = string.Empty;
    }
}

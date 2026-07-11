namespace BudgetControl.Api.DTOs.Accounting
{
    public class AsientoContableFilter
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? Descripcion { get; set; }
        public int? CuentaContableId { get; set; }
        public string? TipoAsiento { get; set; }
        public string? Estado { get; set; }
    }

    public class CrearAsientoContableRequest
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public List<CrearAsientoContableDetalleRequest> Detalles { get; set; } = new();
    }

    public class CrearAsientoContableDetalleRequest
    {
        public int CuentaContableId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
    }

    public class AsientoContableListResponse
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? ModuloOrigen { get; set; }
        public string? IdOrigen { get; set; }
        public decimal TotalDebe { get; set; }
        public decimal TotalHaber { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? AsientoReversionId { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
        public DateTime FechaAlta { get; set; }
    }

    public class AsientoContableResponse : AsientoContableListResponse
    {
        public bool EsAutomatico { get; set; }
        public bool EsReversion { get; set; }
        public int? IdAsientoRevertido { get; set; }
        public List<AsientoContableDetalleResponse> Detalles { get; set; } = new();
    }

    public class AsientoContableDetalleResponse
    {
        public int Id { get; set; }
        public int CuentaContableId { get; set; }
        public string CuentaCodigo { get; set; } = string.Empty;
        public string CuentaNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
    }
}

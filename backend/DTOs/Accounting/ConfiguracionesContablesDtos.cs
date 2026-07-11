namespace BudgetControl.Api.DTOs.Accounting
{
    public class ConfiguracionContableFilter
    {
        public string? CodigoOperacion { get; set; }
        public bool? Activa { get; set; }
    }

    public class UpsertConfiguracionContableRequest
    {
        public string CodigoOperacion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activa { get; set; } = true;
        public List<UpsertConfiguracionContableDetalleRequest> Detalles { get; set; } = new();
    }

    public class UpsertConfiguracionContableDetalleRequest
    {
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public int CuentaContableId { get; set; }
        public int Orden { get; set; }
    }

    public class ConfiguracionContableListResponse
    {
        public int Id { get; set; }
        public string CodigoOperacion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public int CantidadCuentasConfiguradas { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
    }

    public class ConfiguracionContableResponse : ConfiguracionContableListResponse
    {
        public List<ConfiguracionContableDetalleResponse> Detalles { get; set; } = new();
    }

    public class ConfiguracionContableDetalleResponse
    {
        public int Id { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public int CuentaContableId { get; set; }
        public string CuentaCodigo { get; set; } = string.Empty;
        public string CuentaNombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class TipoOperacionContableResponse
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public List<string> ConceptosSugeridos { get; set; } = new();
    }
}

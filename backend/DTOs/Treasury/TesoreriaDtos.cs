namespace BudgetControl.Api.DTOs.Treasury
{
    public class BancoFilterRequest
    {
        public bool? Activo { get; set; }
        public string? Search { get; set; }
    }

    public class UpsertBancoRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }

    public class BancoResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class CuentaBancariaEmpresaFilterRequest
    {
        public int? BancoId { get; set; }
        public string? MonedaCodigo { get; set; }
        public bool? Activa { get; set; }
        public string? Search { get; set; }
    }

    public class UpsertCuentaBancariaEmpresaRequest
    {
        public int BancoId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string MonedaCodigo { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string? Cbu { get; set; }
        public string? AliasCbu { get; set; }
        public int CuentaContableId { get; set; }
        public bool Activa { get; set; } = true;
    }

    public class CuentaBancariaEmpresaResponse
    {
        public int Id { get; set; }
        public int BancoId { get; set; }
        public string BancoCodigo { get; set; } = string.Empty;
        public string BancoNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public string MonedaCodigo { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
        public string? Cbu { get; set; }
        public string? AliasCbu { get; set; }
        public int CuentaContableId { get; set; }
        public string CuentaContableCodigo { get; set; } = string.Empty;
        public string CuentaContableNombre { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}

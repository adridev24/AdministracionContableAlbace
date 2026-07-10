namespace BudgetControl.Api.DTOs.Accounting
{
    public class CuentaContableFilter
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? TipoCuenta { get; set; }
        public bool? Activa { get; set; }
    }

    public class CreateCuentaContableRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
    }

    public class UpdateCuentaContableRequest
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public bool Activa { get; set; }
    }

    public class CuentaContableResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
    }
}

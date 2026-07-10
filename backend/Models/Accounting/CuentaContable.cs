namespace BudgetControl.Api.Models.Accounting
{
    public class CuentaContable
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public bool Activa { get; set; } = true;
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
    }
}

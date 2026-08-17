namespace BudgetControl.Api.Models.Accounting
{
    public class ConfiguracionContableDetalle
    {
        public int Id { get; set; }
        public int ConfiguracionContableId { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public int CuentaContableId { get; set; }
        public int Orden { get; set; }
        public bool EsObligatorio { get; set; } = true;
        public bool Activo { get; set; } = true;

        public ConfiguracionContable ConfiguracionContable { get; set; } = null!;
        public CuentaContable CuentaContable { get; set; } = null!;
    }
}

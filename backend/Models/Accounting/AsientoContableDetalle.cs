namespace BudgetControl.Api.Models.Accounting
{
    public class AsientoContableDetalle
    {
        public int Id { get; set; }
        public int AsientoContableId { get; set; }
        public int CuentaContableId { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public string Descripcion { get; set; } = string.Empty;

        public AsientoContable AsientoContable { get; set; } = null!;
        public CuentaContable CuentaContable { get; set; } = null!;
    }
}

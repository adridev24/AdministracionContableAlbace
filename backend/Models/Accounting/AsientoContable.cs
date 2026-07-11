namespace BudgetControl.Api.Models.Accounting
{
    public class AsientoContable
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? ModuloOrigen { get; set; }
        public string? IdOrigen { get; set; }
        public bool EsAutomatico { get; set; }
        public bool EsReversion { get; set; }
        public int? IdAsientoRevertido { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;

        public AsientoContable? AsientoRevertido { get; set; }
        public ICollection<AsientoContable> Reversiones { get; set; } = new List<AsientoContable>();
        public ICollection<AsientoContableDetalle> Detalles { get; set; } = new List<AsientoContableDetalle>();
    }
}

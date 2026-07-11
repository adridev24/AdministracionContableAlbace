namespace BudgetControl.Api.Models.Accounting
{
    public class ConfiguracionContable
    {
        public int Id { get; set; }
        public string CodigoOperacion { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activa { get; set; } = true;
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;

        public ICollection<ConfiguracionContableDetalle> Detalles { get; set; } = new List<ConfiguracionContableDetalle>();
    }
}

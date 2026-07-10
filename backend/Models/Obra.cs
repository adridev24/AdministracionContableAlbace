namespace BudgetControl.Api.Models
{
    public class Obra
    {
        public int IdObra { get; set; }
        public string NombreObra { get; set; } = null!;
        public string? Descripcion { get; set; }
        public string? Finalizada { get; set; }
        public int ClienteId { get; set; }
        public Client? Cliente { get; set; }
    }
}

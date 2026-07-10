namespace BudgetControl.Api.Models.Commercial
{
    public class ObraReferencia
    {
        public int Id { get; set; }
        public string ObraExternaId { get; set; } = null!;
        public string ClienteExternoId { get; set; } = null!;
        public string NombreObra { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool Activa { get; set; } = true;
    }
}

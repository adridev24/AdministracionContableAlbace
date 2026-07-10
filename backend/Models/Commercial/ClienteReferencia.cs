namespace BudgetControl.Api.Models.Commercial
{
    public class ClienteReferencia
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Documento { get; set; } = null!;
        public bool Activo { get; set; } = true;
    }
}

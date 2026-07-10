namespace BudgetControl.Api.Models
{
    public class Client
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = null!;
        public string? Domicilio { get; set; }
        public string? Telefonoc { get; set; }
        public ICollection<Obra>? Obras { get; set; }
    }
}

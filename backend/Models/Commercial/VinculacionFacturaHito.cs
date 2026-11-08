using BudgetControl.Api.Models.Sales;
namespace BudgetControl.Api.Models.Commercial;
public class VinculacionFacturaHito
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int HitoId { get; set; }
    public int Orden { get; set; }
    public decimal Importe { get; set; }
    public DateTime FechaAlta { get; set; }
    public string UsuarioAlta { get; set; } = null!;
    public Venta Venta { get; set; } = null!;
    public HitoComercialVia Hito { get; set; } = null!;
}

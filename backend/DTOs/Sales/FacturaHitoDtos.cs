namespace BudgetControl.Api.DTOs.Sales;
public class FacturaHitoRequest { public int HitoId { get; set; } public decimal Importe { get; set; } }
public class FacturaHitoResponse : FacturaHitoRequest
{
    public int ViaId { get; set; }
    public int AcuerdoId { get; set; }
    public string NumeroAcuerdo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string MonedaCodigo { get; set; } = null!;
}

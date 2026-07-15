namespace BudgetControl.Api.Models.Sales
{
    public enum VentaEstado
    {
        Borrador,
        Confirmada,
        Anulada
    }

    public enum TipoTratamientoIvaVenta
    {
        Gravado,
        Exento,
        NoGravado
    }

    public enum TipoBaseCalculoPercepcionIibb
    {
        NetoGravado,
        NetoTotal,
        TotalSinIva,
        OtraBaseConfigurable
    }
}

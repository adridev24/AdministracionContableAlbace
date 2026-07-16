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

    public enum SituacionPercepcionIibbCliente
    {
        Pendiente,
        NoAlcanzado,
        Alcanzado,
        Excluido
    }

    public enum ResultadoPercepcionIibb
    {
        Aplicada,
        NoCorresponde,
        Excluido,
        SinRegimen,
        RegimenVencido,
        ClienteSinConfigurar,
        BaseInferiorMinimo,
        Exportacion,
        BaseNoSoportada
    }
}

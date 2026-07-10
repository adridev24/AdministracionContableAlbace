namespace BudgetControl.Api.Models.Commercial
{
    public enum AcuerdoEstado
    {
        Borrador,
        Aprobado,
        EnCurso,
        Finalizado,
        Anulado
    }

    public enum ViaOperacion
    {
        Via1,
        Via2
    }

    public enum ModalidadCobro
    {
        Planificada,
        Abierta
    }

    public enum TipoCuota
    {
        Anticipo,
        Cuota,
        Refuerzo,
        Ajuste,
        Adicional
    }

    public enum CuotaEstado
    {
        Pendiente,
        Parcial,
        Pagada,
        Vencida,
        Anulada
    }

    public enum TipoAjuste
    {
        CambioImporte,
        CambioVencimiento,
        CambioImporteYVencimiento,
        NuevaCuota,
        AnulacionCuota
    }

    public enum TipoAjusteVia
    {
        CambioMonto,
        RefinanciacionAutomatica,
        CambioMoneda,
        AnulacionVia,
        ReactivacionVia
    }

    public enum PagoEstado
    {
        Registrado,
        Aplicado,
        Anulado
    }

    public enum OrigenPago
    {
        Comercial,
        Ventas
    }

    public enum TipoImputacion
    {
        Anticipo,
        PagoParcial,
        Hito,
        SaldoGeneral,
        Cuota
    }

    public enum HitoEstado
    {
        Pendiente,
        Parcial,
        Cumplido,
        Anulado
    }
}

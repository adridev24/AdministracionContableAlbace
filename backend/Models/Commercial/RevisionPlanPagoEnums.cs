namespace BudgetControl.Api.Models.Commercial
{
    public enum TipoRevisionPlanPago
    {
        Modificacion = 1,
        Reduccion = 2,
        Ampliacion = 3,
        Mixta = 4
    }

    public enum OperacionRevisionPlanPago
    {
        Modificada = 1,
        Agregada = 2,
        Eliminada = 3,
        Excluida = 4
    }
}

using BudgetControl.Api.DTOs.Commercial;

namespace BudgetControl.Api.Services.Commercial
{
    public interface IPagoComercialService
    {
        Task<PagoComercialResponse> RegistrarPagoAsync(CreatePagoComercialRequest request);
        Task<PagoComercialResponse> AplicarPagoAsync(int pagoId, AplicarPagoRequest request);
        Task<IEnumerable<AplicacionPagoResponse>> GetAplicacionesPorCuotaAsync(int cuotaId);
    }
}

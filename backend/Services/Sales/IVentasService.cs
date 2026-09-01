using BudgetControl.Api.DTOs.Sales;

namespace BudgetControl.Api.Services.Sales
{
    public interface IVentasService
    {
        Task<IEnumerable<TipoComprobanteVentaResponse>> GetTiposComprobanteAsync(bool soloActivos = false);
        Task<TipoComprobanteVentaResponse?> GetTipoComprobanteAsync(int id);
        Task<TipoComprobanteVentaResponse> CreateTipoComprobanteAsync(TipoComprobanteVentaRequest request);
        Task<TipoComprobanteVentaResponse> UpdateTipoComprobanteAsync(int id, TipoComprobanteVentaRequest request);
        Task<IEnumerable<PuntoVentaResponse>> GetPuntosVentaAsync(bool soloActivos = false);
        Task<PuntoVentaResponse?> GetPuntoVentaAsync(int id);
        Task<PuntoVentaResponse> CreatePuntoVentaAsync(PuntoVentaRequest request);
        Task<PuntoVentaResponse> UpdatePuntoVentaAsync(int id, PuntoVentaRequest request);
        Task<IEnumerable<PuntoVentaComprobanteResponse>> GetComprobantesPorPuntoVentaAsync(int puntoVentaId, bool soloActivos = false);
        Task<IEnumerable<PuntoVentaSelectorResponse>> GetPuntosVentaPorComprobanteAsync(int tipoComprobanteVentaId, int? relacionActualId = null);
        Task<PuntoVentaComprobanteResponse> CreatePuntoVentaComprobanteAsync(int puntoVentaId, PuntoVentaComprobanteRequest request);
        Task<PuntoVentaComprobanteResponse> UpdatePuntoVentaComprobanteAsync(int puntoVentaId, int relacionId, PuntoVentaComprobanteRequest request);
        Task<IEnumerable<AlicuotaIvaVentaResponse>> GetAlicuotasIvaAsync(bool soloActivos = false, string? search = null);
        Task<AlicuotaIvaVentaResponse?> GetAlicuotaIvaAsync(int id);
        Task<AlicuotaIvaVentaResponse> CreateAlicuotaIvaAsync(AlicuotaIvaVentaRequest request);
        Task<AlicuotaIvaVentaResponse> UpdateAlicuotaIvaAsync(int id, AlicuotaIvaVentaRequest request);
        Task<IEnumerable<NomencladorFceResponse>> GetNomencladoresFceAsync(bool soloActivos = false, string? search = null);
        Task<NomencladorFceResponse?> GetNomencladorFceAsync(int id);
        Task<NomencladorFceResponse> CreateNomencladorFceAsync(NomencladorFceRequest request);
        Task<NomencladorFceResponse> UpdateNomencladorFceAsync(int id, NomencladorFceRequest request);
        Task<IEnumerable<PercepcionIibbEntreRiosResponse>> GetPercepcionesIibbAsync(bool soloActivos = false, string? search = null, bool? soloVigentes = null);
        Task<PercepcionIibbEntreRiosResponse?> GetPercepcionIibbAsync(int id);
        Task<PercepcionIibbEntreRiosResponse> CreatePercepcionIibbAsync(PercepcionIibbEntreRiosRequest request);
        Task<PercepcionIibbEntreRiosResponse> UpdatePercepcionIibbAsync(int id, PercepcionIibbEntreRiosRequest request);
        Task<IEnumerable<CategoriaItemFacturableResponse>> GetCategoriasItemsFacturablesAsync(bool soloActivos = false, string? search = null);
        Task<CategoriaItemFacturableResponse?> GetCategoriaItemFacturableAsync(int id);
        Task<CategoriaItemFacturableResponse> CreateCategoriaItemFacturableAsync(CategoriaItemFacturableRequest request);
        Task<CategoriaItemFacturableResponse> UpdateCategoriaItemFacturableAsync(int id, CategoriaItemFacturableRequest request);
        Task<IEnumerable<UnidadMedidaVentaResponse>> GetUnidadesMedidaAsync(bool soloActivos = false, string? search = null);
        Task<UnidadMedidaVentaResponse?> GetUnidadMedidaAsync(int id);
        Task<UnidadMedidaVentaResponse> CreateUnidadMedidaAsync(UnidadMedidaVentaRequest request);
        Task<UnidadMedidaVentaResponse> UpdateUnidadMedidaAsync(int id, UnidadMedidaVentaRequest request);
        Task<IEnumerable<ItemFacturableResponse>> GetItemsFacturablesAsync(bool soloActivos = false, string? search = null, int? categoriaId = null, int? unidadMedidaId = null, int? tratamientoIvaId = null, int? nomencladorId = null);
        Task<ItemFacturableResponse?> GetItemFacturableAsync(int id);
        Task<ItemFacturableResponse> CreateItemFacturableAsync(ItemFacturableRequest request);
        Task<ItemFacturableResponse> UpdateItemFacturableAsync(int id, ItemFacturableRequest request);
        Task<VentaListResponse> GetVentasAsync(VentaListFilterRequest filters);
        Task<VentaResponse?> GetVentaAsync(int id);
        Task<VentaResponse> CreateVentaAsync(VentaHeaderRequest request);
        Task<VentaResponse> UpdateVentaAsync(int id, VentaHeaderRequest request);
        Task<IEnumerable<VentaDetalleResponse>> GetDetallesAsync(int ventaId);
        Task<VentaDetalleMutationResponse> CreateDetalleAsync(int ventaId, VentaDetalleRequest request);
        Task<VentaDetalleMutationResponse> UpdateDetalleAsync(int ventaId, int detalleId, VentaDetalleRequest request);
        Task<VentaResponse> DeleteDetalleAsync(int ventaId, int detalleId);
        Task<IEnumerable<VentaObligacionVia1DisponibleResponse>> GetObligacionesVia1DisponiblesAsync(int ventaId);
        Task<IEnumerable<VentaVinculacionPlanResponse>> GetVinculacionesPlanAsync(int ventaId);
        Task<IEnumerable<VentaVinculacionPlanResponse>> UpdateVinculacionesPlanAsync(int ventaId, IEnumerable<VentaVinculacionPlanRequest> request);
        Task<VentaConfirmacionValidacionResponse> ValidarConfirmacionAsync(int ventaId);
        Task<VentaConfirmacionResponse> ConfirmarVentaAsync(int ventaId);
    }
}

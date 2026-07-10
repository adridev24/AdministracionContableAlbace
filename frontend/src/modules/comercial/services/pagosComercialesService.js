import apiClient from '../../../shared/api/apiClient';

const pagosComercialesService = {
  registrarPago: (payload) => {
    const viaId = payload.acuerdoComercialViaId;
    const url = viaId ? `/api/comercial/acuerdos-vias/${viaId}/pagos` : '/api/pagos-comerciales';
    return apiClient.post(url, payload).then((res) => res.data);
  },
  aplicarPago: (pagoId, payload) => apiClient.post(`/api/pagos-comerciales/${pagoId}/aplicar`, payload).then((res) => res.data),
  getAplicacionesPorCuota: (cuotaId) => apiClient.get(`/api/pagos-comerciales/cuota/${cuotaId}/aplicaciones`).then((res) => res.data)
};

export default pagosComercialesService;

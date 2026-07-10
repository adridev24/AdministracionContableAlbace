import apiClient from '../../../shared/api/apiClient';

const acuerdosService = {
  createAcuerdo: (payload) => apiClient.post('/api/acuerdos', payload).then((res) => res.data),
  getAcuerdoDetalle: (id) => apiClient.get(`/api/acuerdos/${id}`).then((res) => res.data),
  getAcuerdosPorCliente: (clienteExternoId) => apiClient.get(`/api/acuerdos/cliente/${clienteExternoId}`).then((res) => res.data),
  getAcuerdosPorObra: (obraExternaId) => apiClient.get(`/api/acuerdos/obra/${obraExternaId}`).then((res) => res.data),
  crearPlanPago: (acuerdoId, payload) => apiClient.post(`/api/acuerdos/${acuerdoId}/plan-pago`, payload).then((res) => res.data),
  actualizarPlanPago: (acuerdoId, payload) => apiClient.put(`/api/acuerdos/${acuerdoId}/plan-pago`, payload).then((res) => res.data),
  crearPlanPagoVia: (viaId, payload) => apiClient.post(`/api/comercial/acuerdos-vias/${viaId}/plan-pago`, payload).then((res) => res.data),
  actualizarPlanPagoVia: (viaId, payload) => apiClient.put(`/api/comercial/acuerdos-vias/${viaId}/plan-pago`, payload).then((res) => res.data),
  modificarMontoVia: (viaId, payload) => apiClient.put(`/api/comercial/acuerdos-vias/${viaId}/modificar-monto`, payload).then((res) => res.data),
  crearHitoVia: (viaId, payload) => apiClient.post(`/api/comercial/acuerdos-vias/${viaId}/hitos`, payload).then((res) => res.data),
  getHitosVia: (viaId) => apiClient.get(`/api/comercial/acuerdos-vias/${viaId}/hitos`).then((res) => res.data),
  aprobarAcuerdo: (acuerdoId) => apiClient.post(`/api/acuerdos/${acuerdoId}/aprobar`, {}).then((res) => res.data),
  getEstadoComercial: (acuerdoId) => apiClient.get(`/api/acuerdos/${acuerdoId}/estado-comercial`).then((res) => res.data),
  getEstadoComercialVia: (viaId) => apiClient.get(`/api/comercial/acuerdos-vias/${viaId}/estado-comercial`).then((res) => res.data),
  ajustarCuota: (cuotaId, payload) => apiClient.put(`/api/comercial/cuotas/${cuotaId}/ajustar`, payload).then((res) => res.data),
  agregarCuotaAjuste: (planPagoId, payload) => apiClient.post(`/api/comercial/planes/${planPagoId}/cuotas-ajuste`, payload).then((res) => res.data),
  getHistorialAjustesCuota: (cuotaId) => apiClient.get(`/api/comercial/cuotas/${cuotaId}/historial-ajustes`).then((res) => res.data),
  getHistorialAjustesAcuerdo: (acuerdoId) => apiClient.get(`/api/comercial/acuerdos/${acuerdoId}/historial-ajustes`).then((res) => res.data),
  getHistorialAjustesVia: (viaId) => apiClient.get(`/api/comercial/acuerdos-vias/${viaId}/historial-ajustes`).then((res) => res.data),
  getSaldoComercialCliente: (clienteExternoId) => apiClient.get(`/api/clientes/${clienteExternoId}/saldo-comercial`).then((res) => res.data),
  getSaldoComercialObra: (obraExternoId) => apiClient.get(`/api/obras/${obraExternoId}/saldo-comercial`).then((res) => res.data)
};

export default acuerdosService;

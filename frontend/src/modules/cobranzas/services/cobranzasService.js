import apiClient from '../../../shared/api/apiClient';

const buildQuery = (filters = {}) => {
  const params = new URLSearchParams();
  Object.entries(filters).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      params.append(key, value);
    }
  });
  const query = params.toString();
  return query ? `?${query}` : '';
};

const cobranzasService = {
  getMediosPago: (soloActivos = false) =>
    apiClient.get(`/api/cobranzas/medios-pago${soloActivos ? '?soloActivos=true' : ''}`).then((res) => res.data),
  getBancos: (soloActivos = false) =>
    apiClient.get(`/api/cobranzas/bancos${soloActivos ? '?soloActivos=true' : ''}`).then((res) => res.data),
  getCobranzas: (filters = {}) => apiClient.get(`/api/cobranzas${buildQuery(filters)}`).then((res) => res.data),
  getCobranza: (id) => apiClient.get(`/api/cobranzas/${id}`).then((res) => res.data),
  createCobranza: (payload) => apiClient.post('/api/cobranzas', payload).then((res) => res.data),
  updateCobranza: (id, payload) => apiClient.put(`/api/cobranzas/${id}`, payload).then((res) => res.data),
  getFacturasDisponibles: (id) => apiClient.get(`/api/cobranzas/${id}/facturas-disponibles`).then((res) => res.data),
  getAplicaciones: (id) => apiClient.get(`/api/cobranzas/${id}/aplicaciones`).then((res) => res.data),
  addMedio: (id, payload) => apiClient.post(`/api/cobranzas/${id}/medios`, payload).then((res) => res.data),
  updateMedio: (id, medioId, payload) => apiClient.put(`/api/cobranzas/${id}/medios/${medioId}`, payload).then((res) => res.data),
  deleteMedio: (id, medioId) => apiClient.delete(`/api/cobranzas/${id}/medios/${medioId}`).then((res) => res.data),
  addAplicacion: (id, payload) => apiClient.post(`/api/cobranzas/${id}/aplicaciones`, payload).then((res) => res.data),
  updateAplicacion: (id, aplicacionId, payload) => apiClient.put(`/api/cobranzas/${id}/aplicaciones/${aplicacionId}`, payload).then((res) => res.data),
  deleteAplicacion: (id, aplicacionId) => apiClient.delete(`/api/cobranzas/${id}/aplicaciones/${aplicacionId}`).then((res) => res.data),
  confirmar: (id) => apiClient.post(`/api/cobranzas/${id}/confirmar`).then((res) => res.data),
};

export default cobranzasService;

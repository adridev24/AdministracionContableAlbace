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

const ventasService = {
  getTiposComprobante: (soloActivos = false) =>
    apiClient.get(`/api/ventas/tipos-comprobante${soloActivos ? '?soloActivos=true' : ''}`).then((res) => res.data),
  getConfiguracionesComprobante: (filters = {}) =>
    apiClient.get(`/api/ventas/configuraciones-comprobante${buildQuery(filters)}`).then((res) => res.data),
  getConfiguracionComprobante: (id) =>
    apiClient.get(`/api/ventas/configuraciones-comprobante/${id}`).then((res) => res.data),
  createConfiguracionComprobante: (payload) =>
    apiClient.post('/api/ventas/configuraciones-comprobante', payload).then((res) => res.data),
  updateConfiguracionComprobante: (id, payload) =>
    apiClient.put(`/api/ventas/configuraciones-comprobante/${id}`, payload).then((res) => res.data),
  getPuntosVenta: (filters = {}) =>
    apiClient.get(`/api/ventas/puntos-venta${buildQuery(filters)}`).then((res) => res.data),
  getPuntoVenta: (id) => apiClient.get(`/api/ventas/puntos-venta/${id}`).then((res) => res.data),
  createPuntoVenta: (payload) => apiClient.post('/api/ventas/puntos-venta', payload).then((res) => res.data),
  updatePuntoVenta: (id, payload) => apiClient.put(`/api/ventas/puntos-venta/${id}`, payload).then((res) => res.data),
  getComprobantesPorPuntoVenta: (puntoVentaId, filters = {}) =>
    apiClient.get(`/api/ventas/puntos-venta/${puntoVentaId}/comprobantes${buildQuery(filters)}`).then((res) => res.data),
  createPuntoVentaComprobante: (puntoVentaId, payload) =>
    apiClient.post(`/api/ventas/puntos-venta/${puntoVentaId}/comprobantes`, payload).then((res) => res.data),
  updatePuntoVentaComprobante: (puntoVentaId, relacionId, payload) =>
    apiClient.put(`/api/ventas/puntos-venta/${puntoVentaId}/comprobantes/${relacionId}`, payload).then((res) => res.data),
  getAlicuotasIva: (filters = {}) =>
    apiClient.get(`/api/ventas/alicuotas-iva${buildQuery(filters)}`).then((res) => res.data),
  getAlicuotaIva: (id) => apiClient.get(`/api/ventas/alicuotas-iva/${id}`).then((res) => res.data),
  createAlicuotaIva: (payload) => apiClient.post('/api/ventas/alicuotas-iva', payload).then((res) => res.data),
  updateAlicuotaIva: (id, payload) => apiClient.put(`/api/ventas/alicuotas-iva/${id}`, payload).then((res) => res.data),
  getNomencladores: (filters = {}) =>
    apiClient.get(`/api/ventas/nomencladores${buildQuery(filters)}`).then((res) => res.data),
  getNomenclador: (id) => apiClient.get(`/api/ventas/nomencladores/${id}`).then((res) => res.data),
  createNomenclador: (payload) => apiClient.post('/api/ventas/nomencladores', payload).then((res) => res.data),
  updateNomenclador: (id, payload) => apiClient.put(`/api/ventas/nomencladores/${id}`, payload).then((res) => res.data),
  getPercepcionesIibb: (filters = {}) =>
    apiClient.get(`/api/ventas/percepciones-iibb${buildQuery(filters)}`).then((res) => res.data),
  getPercepcionIibb: (id) => apiClient.get(`/api/ventas/percepciones-iibb/${id}`).then((res) => res.data),
  createPercepcionIibb: (payload) => apiClient.post('/api/ventas/percepciones-iibb', payload).then((res) => res.data),
  updatePercepcionIibb: (id, payload) => apiClient.put(`/api/ventas/percepciones-iibb/${id}`, payload).then((res) => res.data),
  getVentas: (filters) => apiClient.get(`/api/ventas${buildQuery(filters)}`).then((res) => res.data),
  getVenta: (id) => apiClient.get(`/api/ventas/${id}`).then((res) => res.data),
  createVenta: (payload) => apiClient.post('/api/ventas', payload).then((res) => res.data),
  updateVenta: (id, payload) => apiClient.put(`/api/ventas/${id}`, payload).then((res) => res.data),
};

export default ventasService;

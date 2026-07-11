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
  getVentas: (filters) => apiClient.get(`/api/ventas${buildQuery(filters)}`).then((res) => res.data),
  getVenta: (id) => apiClient.get(`/api/ventas/${id}`).then((res) => res.data),
  createVenta: (payload) => apiClient.post('/api/ventas', payload).then((res) => res.data),
  updateVenta: (id, payload) => apiClient.put(`/api/ventas/${id}`, payload).then((res) => res.data),
};

export default ventasService;

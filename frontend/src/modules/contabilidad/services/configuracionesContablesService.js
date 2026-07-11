import apiClient from '../../../shared/api/apiClient';

const configuracionesContablesService = {
  getTiposOperacion: () => apiClient.get('/api/contabilidad/configuraciones/tipos-operacion').then((res) => res.data),
  getConfiguraciones: (filters = {}) => apiClient.get('/api/contabilidad/configuraciones', { params: filters }).then((res) => res.data),
  getConfiguracion: (id) => apiClient.get(`/api/contabilidad/configuraciones/${id}`).then((res) => res.data),
  getConfiguracionPorOperacion: (codigoOperacion) => apiClient.get(`/api/contabilidad/configuraciones/operacion/${codigoOperacion}`).then((res) => res.data),
  createConfiguracion: (payload) => apiClient.post('/api/contabilidad/configuraciones', payload).then((res) => res.data),
  updateConfiguracion: (id, payload) => apiClient.put(`/api/contabilidad/configuraciones/${id}`, payload).then((res) => res.data),
  deactivateConfiguracion: (id) => apiClient.delete(`/api/contabilidad/configuraciones/${id}`).then((res) => res.data),
};

export default configuracionesContablesService;

import apiClient from '../../../shared/api/apiClient';

const asientosContablesService = {
  getAsientos: (filters = {}) => apiClient.get('/api/contabilidad/asientos', { params: filters }).then((res) => res.data),
  getAsiento: (id) => apiClient.get(`/api/contabilidad/asientos/${id}`).then((res) => res.data),
  createAsiento: (payload) => apiClient.post('/api/contabilidad/asientos', payload).then((res) => res.data),
  reversarAsiento: (id) => apiClient.post(`/api/contabilidad/asientos/${id}/reversar`, {}).then((res) => res.data),
};

export default asientosContablesService;

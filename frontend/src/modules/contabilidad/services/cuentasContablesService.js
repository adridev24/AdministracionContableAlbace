import apiClient from '../../../shared/api/apiClient';

const cuentasContablesService = {
  getCuentas: (filters = {}) => apiClient.get('/api/contabilidad/cuentas', { params: filters }).then((res) => res.data),
  getCuenta: (id) => apiClient.get(`/api/contabilidad/cuentas/${id}`).then((res) => res.data),
  createCuenta: (payload) => apiClient.post('/api/contabilidad/cuentas', payload).then((res) => res.data),
  updateCuenta: (id, payload) => apiClient.put(`/api/contabilidad/cuentas/${id}`, payload).then((res) => res.data),
  deactivateCuenta: (id) => apiClient.delete(`/api/contabilidad/cuentas/${id}`).then((res) => res.data),
};

export default cuentasContablesService;

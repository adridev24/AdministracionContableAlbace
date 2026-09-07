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

const tesoreriaService = {
  getBancos: (filters = {}) => apiClient.get(`/api/tesoreria/bancos${buildQuery(filters)}`).then((res) => res.data),
  createBanco: (payload) => apiClient.post('/api/tesoreria/bancos', payload).then((res) => res.data),
  updateBanco: (id, payload) => apiClient.put(`/api/tesoreria/bancos/${id}`, payload).then((res) => res.data),
  getCuentasBancarias: (filters = {}) => apiClient.get(`/api/tesoreria/cuentas-bancarias${buildQuery(filters)}`).then((res) => res.data),
  createCuentaBancaria: (payload) => apiClient.post('/api/tesoreria/cuentas-bancarias', payload).then((res) => res.data),
  updateCuentaBancaria: (id, payload) => apiClient.put(`/api/tesoreria/cuentas-bancarias/${id}`, payload).then((res) => res.data),
};

export default tesoreriaService;

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

const cuentaCorrienteClientesService = {
  exportarExcel: (clienteId, filters) => apiClient.get(`/api/cuenta-corriente-clientes/${clienteId}/excel${buildQuery(filters)}`, { responseType: 'blob' }).then(res => res.data),
  getCuentaCorriente: (clienteId, filters = {}) =>
    apiClient.get(`/api/cuenta-corriente-clientes/${clienteId}${buildQuery(filters)}`).then((res) => res.data),
};

export default cuentaCorrienteClientesService;

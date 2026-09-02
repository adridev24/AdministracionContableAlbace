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
  getCuentaCorriente: (clienteId, filters = {}) =>
    apiClient.get(`/api/cuenta-corriente-clientes/${clienteId}${buildQuery(filters)}`).then((res) => res.data),
};

export default cuentaCorrienteClientesService;

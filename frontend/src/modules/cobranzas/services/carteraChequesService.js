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

const carteraChequesService = {
  getCheques: (filters = {}) => apiClient.get(`/api/cartera-cheques${buildQuery(filters)}`).then((res) => res.data),
  getCheque: (id) => apiClient.get(`/api/cartera-cheques/${id}`).then((res) => res.data),
  depositar: (id, payload) => apiClient.post(`/api/cartera-cheques/${id}/depositar`, payload).then((res) => res.data),
  acreditar: (id, payload) => apiClient.post(`/api/cartera-cheques/${id}/acreditar`, payload).then((res) => res.data),
};

export default carteraChequesService;

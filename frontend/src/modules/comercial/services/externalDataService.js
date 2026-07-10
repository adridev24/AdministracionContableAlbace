import apiClient from '../../../shared/api/apiClient';

const externalDataService = {
  getClients: () => apiClient.get('/api/ExternalData/clients').then((res) => res.data),
  getClientById: (clienteId) => apiClient.get(`/api/ExternalData/clients/${clienteId}`).then((res) => res.data),
  getObrasByClient: (clienteId) => apiClient.get(`/api/ExternalData/clients/${clienteId}/obras`).then((res) => res.data),
  getObras: () => apiClient.get('/api/ExternalData/obras').then((res) => res.data),
  getObraById: (obraId) => apiClient.get(`/api/ExternalData/obras/${obraId}`).then((res) => res.data)
};

export default externalDataService;

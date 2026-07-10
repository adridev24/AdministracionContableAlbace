import apiClient from '../../../shared/api/apiClient';

const cuotasComercialesService = {
  getCuotasVencidas: () => apiClient.get('/api/cuotas/vencidas').then((res) => res.data),
  getCuotasPendientes: () => apiClient.get('/api/cuotas/pendientes').then((res) => res.data)
};

export default cuotasComercialesService;

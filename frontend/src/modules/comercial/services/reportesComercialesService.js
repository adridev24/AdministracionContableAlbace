import { reportQuery } from './reportFilters.js';
import apiClient from '../../../shared/api/apiClient';

const reportesComercialesService = {
  getResumen: (filters) => {
    const query = reportQuery(filters);
    return apiClient.get(`/api/comercial/reportes/resumen${query ? `?${query}` : ''}`).then((res) => res.data);
  },
};

export default reportesComercialesService;

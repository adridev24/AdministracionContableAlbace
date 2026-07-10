import apiClient from '../../../shared/api/apiClient';

const reportesComercialesService = {
  getResumen: ({ desde, hasta, via } = {}) => {
    const params = new URLSearchParams();
    if (desde) params.set('desde', desde);
    if (hasta) params.set('hasta', hasta);
    if (via && via !== 'Todos') params.set('via', via);

    const query = params.toString();
    return apiClient.get(`/api/comercial/reportes/resumen${query ? `?${query}` : ''}`).then((res) => res.data);
  },
};

export default reportesComercialesService;

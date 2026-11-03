import apiClient from '../../../shared/api/apiClient';

const route = (viaId) => `/api/comercial/acuerdos-vias/${viaId}/plan-pago/revision`;
export const createRevisionPlanService = (client) => ({
  prepare: ({ viaId, acuerdoId, planId }) => client.get(route(viaId), { params: { acuerdoId, planId } }).then(res => res.data),
  confirm: (viaId, request) => client.post(route(viaId), request, { timeout: 30000 }).then(res => res.data)
});
export default createRevisionPlanService(apiClient);

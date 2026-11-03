import { calendarDateValue, calendarDateToApi } from '../../../shared/utils/calendarDate.js';

export const canReviewPlan = (roles, acuerdo, via) => Boolean(roles.includes('Admin') && via?.planPago &&
  via.modalidadCobro === 'Planificada' && !['Anulado', 'Finalizado'].includes(acuerdo?.estado) &&
  !['Anulado', 'Finalizado'].includes(via.estado) && (acuerdo?.estado === 'Aprobado' || via.estado === 'Aprobado'));

// No arithmetic with binary floating point in the preview. Invalid/unsafe values fail closed.
export function cents(value) {
  const match = /^(\d+)(?:[.,](\d{1,2}))?$/.exec(String(value ?? '').trim());
  if (!match) return null;
  const result = BigInt(match[1]) * 100n + BigInt((match[2] || '').padEnd(2, '0'));
  return result <= BigInt(Number.MAX_SAFE_INTEGER) ? result : null;
}
export const decimalText = (value) => value == null ? '' : `${value / 100n}.${String(value % 100n).padStart(2, '0')}`;
export function formatMoney(value, currency = '') {
  if (value == null) return '—';
  const abs = value < 0n ? -value : value;
  return `${currency} ${value < 0n ? '−' : ''}${(abs / 100n).toLocaleString('es-AR')},${String(abs % 100n).padStart(2, '0')}`.trim();
}
function apiMoney(value) {
  const number = Number(decimalText(value));
  if (cents(number) !== value) throw new Error('El importe supera la precisión admitida por esta interfaz.');
  return number;
}
export const newDraft = (data) => ({ data, edited: {}, removed: [], added: [], nuevoMonto: decimalText(cents(data.montoActual)), comentario: '', confirmado: false });
export const orderedQuotas = (draft) => [...draft.data.cuotas].sort((a, b) => a.numero - b.numero || a.id - b.id);
export const quotaValues = (draft, quota) => draft.edited[quota.id] || { importe: decimalText(cents(quota.importe)), vencimiento: calendarDateValue(quota.vencimiento) };
export const isChanged = (draft, quota) => {
  const value = quotaValues(draft, quota);
  return cents(value.importe) !== cents(quota.importe) || value.vencimiento !== calendarDateValue(quota.vencimiento);
};
export const retirementText = (quota) => quota.tieneHistoria ? 'La cuota será anulada y se conservará su historial' : 'La cuota será eliminada';
export function tailQuota(draft) {
  return orderedQuotas(draft).filter(c => c.tipo === 'Cuota' && c.estado !== 'Anulada' && !draft.removed.includes(c.id)).at(-1);
}
export function canRetire(draft, quota) {
  if (quota.tipo !== 'Cuota' || tailQuota(draft)?.id !== quota.id || !quota.puedeModificar) return false;
  if (quota.id === draft.data.ultimaCuotaOrdinariaActivaId) return quota.puedeRetirar === true;
  // The only local relaxation is the documented sequence restriction, after retiring its successors.
  return draft.removed.includes(draft.data.ultimaCuotaOrdinariaActivaId) &&
    quota.motivoBloqueoRetiro === 'Solo puede retirarse la última cuota ordinaria activa.' && !quota.motivoBloqueoModificacion;
}
export function editQuota(draft, id, field, value) {
  const quota = draft.data.cuotas.find(c => c.id === id);
  if (!quota?.puedeModificar || draft.removed.includes(id) || !['importe', 'vencimiento'].includes(field)) return draft;
  return { ...draft, confirmado: false, edited: { ...draft.edited, [id]: { ...quotaValues(draft, quota), [field]: value } } };
}
export function retireQuota(draft, id) {
  const quota = draft.data.cuotas.find(c => c.id === id);
  return quota && canRetire(draft, quota) ? { ...draft, confirmado: false, removed: [...draft.removed, id] } : draft;
}
export function restoreRetirements(draft) { return { ...draft, removed: [], confirmado: false }; }
export function addQuota(draft) {
  return { ...draft, confirmado: false, added: [...draft.added, { numero: draft.data.siguienteNumeroCuota + draft.added.length, importe: '', vencimiento: '' }] };
}
export function editAdded(draft, index, field, value) {
  if (!['importe', 'vencimiento'].includes(field)) return draft;
  return { ...draft, confirmado: false, added: draft.added.map((c, i) => i === index ? { ...c, [field]: value } : c) };
}
export function removeAdded(draft, index) {
  return { ...draft, confirmado: false, added: draft.added.filter((_, i) => i !== index).map((c, i) => ({ ...c, numero: draft.data.siguienteNumeroCuota + i })) };
}
export function preview(draft) {
  const modified = orderedQuotas(draft).filter(c => !draft.removed.includes(c.id) && isChanged(draft, c));
  const retired = orderedQuotas(draft).filter(c => draft.removed.includes(c.id)).reverse();
  const active = orderedQuotas(draft).filter(c => c.estado !== 'Anulada' && !draft.removed.includes(c.id));
  const amounts = [...active.map(c => cents(quotaValues(draft, c).importe)), ...draft.added.map(c => cents(c.importe))];
  const total = amounts.some(x => x == null) ? null : amounts.reduce((a, b) => a + b, 0n);
  const nuevo = cents(draft.nuevoMonto), anterior = cents(draft.data.montoActual), paid = cents(draft.data.totalPagadoValido);
  const errors = [];
  if (nuevo == null) errors.push('Ingrese el nuevo MontoActual, con hasta dos decimales.');
  if (amounts.some(x => x == null)) errors.push('Complete los importes de las cuotas con valores no negativos y hasta dos decimales.');
  if (total !== nuevo || total == null) errors.push('El total de obligaciones debe coincidir exactamente con el nuevo MontoActual.');
  if (!draft.comentario.trim() || draft.comentario.trim().length > 2000) errors.push('Ingrese un comentario de hasta 2000 caracteres.');
  if ([...modified.map(c => quotaValues(draft, c)), ...draft.added].some(c => !calendarDateValue(c.vencimiento) || c.vencimiento < '1900-01-01')) errors.push('Complete los vencimientos con fechas válidas.');
  if (!modified.length && !retired.length && !draft.added.length) errors.push('No hay cambios en las cuotas para confirmar.');
  if ([anterior, paid].some(x => x == null)) errors.push('Los importes recibidos requieren revisión antes de continuar.');
  if (!draft.data.versionVia || !draft.data.versionPlan || draft.data.cuotas.some(c => !c.version)) errors.push('Faltan versiones de preparación. Recargue los datos.');
  try { if (nuevo != null) apiMoney(nuevo); amounts.filter(x => x != null).forEach(apiMoney); } catch (error) { errors.push(error.message); }
  return { modified, retired, added: draft.added, total, nuevo, anterior, paid, difference: nuevo != null && anterior != null ? nuevo - anterior : null,
    pending: nuevo != null && paid != null ? (nuevo > paid ? nuevo - paid : 0n) : null,
    credit: nuevo != null && paid != null ? (paid > nuevo ? paid - nuevo : 0n) : null,
    matches: nuevo != null && total === nuevo, errors, canConfirm: errors.length === 0 && draft.confirmado };
}
export function buildRequest(draft, solicitudId) {
  const view = preview(draft);
  if (!view.canConfirm) throw new Error('Revise los datos y confirme expresamente la operación.');
  const data = draft.data;
  return {
    acuerdoComercialId: data.acuerdoComercialId, acuerdoComercialViaId: data.acuerdoComercialViaId, planPagoId: data.planPagoId,
    nuevoMontoActual: apiMoney(view.nuevo), comentario: draft.comentario.trim(), solicitudId, confirmado: true,
    versionVia: data.versionVia, versionPlan: data.versionPlan,
    cuotasOriginales: data.cuotas.map(c => ({ cuotaId: c.id, version: c.version })),
    cambios: [
      ...view.modified.map(c => { const v = quotaValues(draft, c); return { operacion: 'Modificar', cuotaId: c.id, version: c.version,
        ...(cents(v.importe) !== cents(c.importe) ? { importe: apiMoney(cents(v.importe)) } : {}),
        ...(v.vencimiento !== calendarDateValue(c.vencimiento) ? { vencimiento: calendarDateToApi(v.vencimiento) } : {}) }; }),
      ...view.retired.map(c => ({ operacion: 'Retirar', cuotaId: c.id, version: c.version })),
      ...view.added.map(c => ({ operacion: 'Agregar', tipo: 'Cuota', numero: c.numero, importe: apiMoney(cents(c.importe)), vencimiento: calendarDateToApi(c.vencimiento) }))
    ]
  };
}

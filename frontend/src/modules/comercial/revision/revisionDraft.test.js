import test from 'node:test';
import assert from 'node:assert/strict';
import { canReviewPlan, cents, newDraft, editQuota, isChanged, retireQuota, canRetire, tailQuota, retirementText, addQuota,
  editAdded, removeAdded, preview, buildRequest, formatMoney, restoreRetirements } from './revisionDraft.js';
import { preparationFixture } from './revisionFixtures.js';
import { getUserRoles } from '../../../Services/authUtils.js';

test('exact Admin role from actual JWT claim enables action only on an approved existing plan', () => {
  const agreement = { estado: 'Aprobado' }, via = { estado: 'Aprobado', modalidadCobro: 'Planificada', planPago: { id: 30 } };
  for (const role of ['Admin', 'User', 'Administrador', 'admin']) {
    const payload = { 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role };
    const previous = globalThis.localStorage;
    globalThis.localStorage = { getItem: () => `header.${Buffer.from(JSON.stringify(payload)).toString('base64url')}.signature` };
    try { assert.equal(canReviewPlan(getUserRoles(), agreement, via), role === 'Admin'); } finally { globalThis.localStorage = previous; }
  }
  assert.equal(canReviewPlan(['Admin'], agreement, { ...via, planPago: null }), false);
  assert.equal(canReviewPlan(['Admin'], { estado: 'Borrador' }, { ...via, estado: 'Borrador' }), false);
  assert.equal(canReviewPlan(['Admin'], { estado: 'Finalizado' }, via), false);
});
test('decimal preview uses integer cents with no floating point sum errors', () => {
  assert.equal(cents('0.10') + cents('0,20'), 30n);
  assert.equal(formatMoney(30n, 'ARS'), 'ARS 0,30');
  for (const invalid of ['', '-1', '1.001', '1e4', 'NaN', '900719925474099100']) assert.equal(cents(invalid), null);
});
test('amount/date changes are identified; backend-blocked quota cannot be edited', () => {
  let d = newDraft(preparationFixture());
  d = editQuota(d, 1, 'importe', '90.50'); d = editQuota(d, 1, 'vencimiento', '2027-01-02');
  assert.equal(isChanged(d, d.data.cuotas[1]), true);
  assert.equal(preview(d).total, 59050n);
  d.data.cuotas[2].puedeModificar = false;
  assert.equal(editQuota(d, 2, 'importe', '1'), d);
  assert.equal(editQuota(d, 1, 'numero', 99), d);
});
test('only tail can retire: 5 then 4 then 3, no renumbering or advance retirement', () => {
  let d = newDraft(preparationFixture());
  assert.equal(retireQuota(d, 3), d); assert.equal(retireQuota(d, 0), d);
  for (const id of [5, 4, 3]) { assert.equal(tailQuota(d).id, id); d = retireQuota(d, id); }
  assert.deepEqual(d.removed, [5, 4, 3]);
  assert.deepEqual(d.data.cuotas.map(c => c.numero), [0, 1, 2, 3, 4, 5]);
  assert.equal(preview(d).total, 30000n);
  assert.deepEqual(restoreRetirements(d).removed, []);
});
test('blocked tail cannot be skipped and independent withdrawal restrictions are respected', () => {
  let d = newDraft(preparationFixture());
  d.data.cuotas[5].puedeRetirar = false;
  assert.equal(retireQuota(d, 5), d); assert.equal(retireQuota(d, 4), d);
  d.data.cuotas[5].puedeRetirar = true; d = retireQuota(d, 5);
  d.data.cuotas[4].puedeModificar = false;
  assert.equal(retireQuota(d, 4), d); assert.equal(retireQuota(d, 3), d);
  d.data.cuotas[4].puedeModificar = true; d.data.cuotas[4].motivoBloqueoRetiro = 'Otra restricción';
  assert.equal(canRetire(d, d.data.cuotas[4]), false);
});
test('history determines physical deletion vs logical annulment preview', () => {
  const data = preparationFixture();
  assert.equal(retirementText(data.cuotas[5]), 'La cuota será eliminada');
  assert.match(retirementText(data.cuotas[4]), /anulada.*historial/);
});
test('new ordinary numbers follow server high-water mark; pending additions can be removed locally', () => {
  let d = addQuota(addQuota(newDraft(preparationFixture())));
  assert.deepEqual(d.added.map(c => c.numero), [9, 10]);
  assert.equal(editAdded(d, 0, 'tipo', 'Adicional'), d);
  assert.equal(editAdded(d, 0, 'numero', 2), d);
  d = editAdded(d, 1, 'importe', '50'); d = removeAdded(d, 0);
  assert.deepEqual(d.added, [{ numero: 9, importe: '50', vencimiento: '' }]);
  assert.deepEqual(d.data.cuotas.map(c => c.numero), [0, 1, 2, 3, 4, 5]);
});
test('comment, matching total, changes, valid values and explicit confirmation are mandatory', () => {
  let d = newDraft(preparationFixture()); d.comentario = 'Motivo'; d.confirmado = true;
  assert.equal(preview(d).canConfirm, false);
  d = editQuota(d, 1, 'importe', '90'); assert.equal(preview(d).matches, false);
  d.nuevoMonto = '590'; d.confirmado = true; assert.equal(preview(d).canConfirm, true);
  d.comentario = ' '; assert.equal(preview(d).canConfirm, false);
  d.comentario = 'Motivo'; d.confirmado = false; assert.equal(preview(d).canConfirm, false);
  d = editQuota(d, 1, 'vencimiento', '2026-02-30'); assert.match(preview(d).errors.join(' '), /vencimientos/);
});
test('summary and exact backend DTO retain all IDs/versions without trusting audit fields', () => {
  let d = newDraft(preparationFixture());
  d = editQuota(d, 1, 'importe', '90'); d = editQuota(d, 1, 'vencimiento', '2027-01-01');
  d = retireQuota(retireQuota(d, 5), 4); d = addQuota(d); d = editAdded(d, 0, 'importe', '50'); d = editAdded(d, 0, 'vencimiento', '2027-02-01');
  d.nuevoMonto = '440'; d.comentario = '  Motivo  '; d.confirmado = true;
  const v = preview(d); assert.equal(v.total, 44000n); assert.equal(v.difference, -16000n); assert.equal(v.pending, 36000n); assert.equal(v.credit, 0n);
  assert.deepEqual(buildRequest(d, 'uuid'), {
    acuerdoComercialId: 10, acuerdoComercialViaId: 20, planPagoId: 30, nuevoMontoActual: 440, comentario: 'Motivo', solicitudId: 'uuid', confirmado: true,
    versionVia: 201, versionPlan: 202, cuotasOriginales: d.data.cuotas.map(c => ({ cuotaId: c.id, version: c.version })),
    cambios: [
      { operacion: 'Modificar', cuotaId: 1, version: 301, importe: 90, vencimiento: '2027-01-01T00:00:00.000Z' },
      { operacion: 'Retirar', cuotaId: 5, version: 305 }, { operacion: 'Retirar', cuotaId: 4, version: 304 },
      { operacion: 'Agregar', tipo: 'Cuota', numero: 9, importe: 50, vencimiento: '2027-02-01T00:00:00.000Z' }
    ]
  });
  d.data.totalPagadoValido = 500; assert.equal(preview(d).credit, 6000n); assert.equal(preview(d).pending, 0n);
});

import test from 'node:test';
import assert from 'node:assert/strict';
import { estadoAcuerdosAbiertos } from './estadoAcuerdosAbiertos.js';
const result = rows => ({ key: 'current', rows, error: '' });
test('no open agreement leaves planned invoicing available and no rows to display', () => {
  const state = estadoAcuerdosAbiertos(true, 'current', result([]));
  assert.equal(state.bloqueado, false); assert.equal(state.rows.length, 0); assert.equal(state.mensaje, '');
});
test('every existing open agreement needs a hito regardless of planned obligations', () => {
  const state = estadoAcuerdosAbiertos(true, 'current', result([{ numeroAcuerdo: 'A', hitos: [{ id: 1 }] }, { numeroAcuerdo: 'B', hitos: [] }]));
  assert.equal(state.bloqueado, true); assert.match(state.mensaje, /B/);
});
test('refresh after creating hitos releases the block and exposes the new hitos', () => {
  const state = estadoAcuerdosAbiertos(true, 'current', result([{ numeroAcuerdo: 'A', hitos: [{ id: 1 }] }]));
  assert.equal(state.bloqueado, false); assert.equal(state.rows[0].hitos[0].id, 1);
});
test('stale client or project results cannot enable saving or expose old agreements', () => {
  const state = estadoAcuerdosAbiertos(true, 'new-project', result([{ numeroAcuerdo: 'A', hitos: [{ id: 1 }] }]));
  assert.equal(state.loading, true); assert.equal(state.bloqueado, true); assert.equal(state.rows.length, 0);
});
test('lookup failures block saving instead of being treated as no agreement', () => {
  assert.equal(estadoAcuerdosAbiertos(true, 'current', { ...result([]), error: 'Error' }).bloqueado, true);
  assert.equal(estadoAcuerdosAbiertos(false, 'current', result([])).bloqueado, false);
});

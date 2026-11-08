import test from 'node:test';
import assert from 'node:assert/strict';
import { situacionHitos } from './situacionHitos.js';

test('separates uninvoiced agreement balance from invoiced unpaid balance', () => {
  const s = situacionHitos({ montoActual: 200, hitos: [{ importeEstimado: 100, importeFacturado: 60, importeAplicado: 25 }] });
  assert.equal(s.facturado, 60); assert.equal(s.cobrado, 25);
  assert.equal(s.pendienteFacturar, 140); assert.equal(s.pendienteCobro, 35); assert.equal(s.pendienteTotal, 175);
  assert.equal(s.hitos[0].pendienteFacturar, 40);
  assert.equal(s.hitos[0].estadoFacturacion, 'Parcial'); assert.equal(s.hitos[0].estadoCobranza, 'Parcial');
});
test('draft reservations are separate from confirmed billing and collection', () => {
  const s = situacionHitos({ montoActual: 100, hitos: [{ importeEstimado: 100, importeReservado: 80 }] });
  assert.equal(s.reservado, 80); assert.equal(s.facturado, 0); assert.equal(s.cobrado, 0);
  assert.equal(s.hitos[0].estadoFacturacion, 'Sin facturar'); assert.equal(s.hitos[0].estadoCobranza, 'Sin cobrar');
});
test('fully collected partial invoice keeps billing state partial', () => {
  const s = situacionHitos({ montoActual: 100, hitos: [{ importeEstimado: 100, importeFacturado: 60, importeAplicado: 60 }] });
  assert.equal(s.hitos[0].estadoFacturacion, 'Parcial'); assert.equal(s.hitos[0].estadoCobranza, 'Cobrado');
  assert.equal(s.pendienteCobro, 0); assert.equal(s.pendienteTotal, 40);
});

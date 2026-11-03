import test, { before, after } from 'node:test';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { createServer } from 'vite';
import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { newDraft, retireQuota, editQuota } from './revisionDraft.js';
import { preparationFixture } from './revisionFixtures.js';

let vite, View, serviceFactory;
before(async () => {
  vite = await createServer({ root: fileURLToPath(new URL('../../../../', import.meta.url)), optimizeDeps: { noDiscovery: true, include: [] }, server: { middlewareMode: true }, appType: 'custom' });
  ({ RevisionPlanView: View } = await vite.ssrLoadModule('/src/modules/comercial/components/RevisionPlanPanel.jsx'));
  ({ createRevisionPlanService: serviceFactory } = await vite.ssrLoadModule('/src/modules/comercial/services/revisionPlanService.js'));
});
after(async () => { await vite?.close(); });
const render = (draft = newDraft(preparationFixture()), status = 'ready') => renderToStaticMarkup(React.createElement(View, { state: { draft, status, error: '' }, session: {} }));
test('React renders amounts, balances, separate advance and backend blocking reason', () => {
  const draft = newDraft(preparationFixture());
  draft.data.cuotas[2].puedeModificar = false; draft.data.cuotas[2].motivoBloqueoModificacion = 'Tiene una factura activa.';
  const html = render(draft);
  for (const label of ['MontoOriginal (solo lectura)', 'MontoActual vigente', 'Total pagado válido', 'Saldo pendiente', 'Saldo a favor', 'Anticipo separado', 'Tiene una factura activa.', 'ARS 600,00']) assert.ok(html.includes(label), label);
  assert.match(html, /aria-label="Importe Cuota 2"[^>]*disabled/);
  assert.ok(html.includes('Retirar cuota 5')); assert.ok(!html.includes('Retirar cuota 4')); assert.ok(!html.includes('Retirar cuota 0'));
});
test('React marks changed fields and distinguishes pending physical/logical withdrawals', () => {
  let draft = newDraft(preparationFixture()); draft = editQuota(draft, 1, 'importe', '90');
  draft = retireQuota(retireQuota(draft, 5), 4);
  const html = render(draft);
  for (const label of ['revision-field-changed', 'La cuota será eliminada', 'La cuota será anulada', 'pendiente de confirmar', 'Retirar cuota 3', 'Resumen y confirmación']) assert.ok(html.includes(label), label);
});
test('React renders controlled conflict and forbids confirming or editing stale draft', () => {
  const html = render(newDraft(preparationFixture()), 'conflict');
  assert.ok(html.includes('Recargar datos')); assert.ok(html.includes('Conservamos su borrador'));
  assert.match(html, /aria-label="Importe Cuota 1"[^>]*disabled/);
});

test('new local tail explains its dependency block instead of the obsolete sequence reason', () => {
  let draft = newDraft(preparationFixture());
  draft.data.cuotas[2].puedeModificar = false;
  draft.data.cuotas[2].motivoBloqueoModificacion = 'Tiene una factura activa.';
  draft = retireQuota(retireQuota(retireQuota(draft, 5), 4), 3);
  assert.ok(render(draft).includes('Retiro bloqueado: Tiene una factura activa.'));
});
test('HTTP service centralizes exact endpoints, query IDs, request and timeout', async () => {
  const calls = [];
  const client = { get: async (...args) => { calls.push(['get', ...args]); return { data: 'prepared' }; }, post: async (...args) => { calls.push(['post', ...args]); return { data: 'confirmed' }; } };
  const service = serviceFactory(client), request = { solicitudId: 'same-id' };
  assert.equal(await service.prepare({ viaId: 20, acuerdoId: 10, planId: 30 }), 'prepared');
  assert.equal(await service.confirm(20, request), 'confirmed');
  assert.deepEqual(calls, [
    ['get', '/api/comercial/acuerdos-vias/20/plan-pago/revision', { params: { acuerdoId: 10, planId: 30 } }],
    ['post', '/api/comercial/acuerdos-vias/20/plan-pago/revision', request, { timeout: 30000 }]
  ]);
});

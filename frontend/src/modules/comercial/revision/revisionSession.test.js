import test from 'node:test';
import assert from 'node:assert/strict';
import { createRevisionSession, requestId } from './revisionSession.js';
import { editQuota } from './revisionDraft.js';
import { preparationFixture } from './revisionFixtures.js';

const failure = (status) => ({ response: { status, data: { error: `Error ${status}` } } });
async function ready(confirm, onSuccess) {
  let gets = 0, ids = 0;
  const session = createRevisionSession({ target: { viaId: 20, acuerdoId: 10, planId: 30 },
    api: { prepare: async () => { gets++; return preparationFixture(); }, confirm }, onSuccess, uuid: () => `id-${++ids}` });
  await session.load();
  session.edit(d => ({ ...editQuota(d, 1, 'vencimiento', '2027-01-01'), comentario: 'Motivo', confirmado: true }));
  return { session, gets: () => gets, ids: () => ids };
}
test('secure UUID uses randomUUID or cryptographic getRandomValues; never Math.random', () => {
  assert.equal(requestId({ randomUUID: () => 'native' }), 'native');
  assert.match(requestId({ getRandomValues: b => b.fill(1) }), /^[\da-f]{8}-[\da-f]{4}-4[\da-f]{3}-[89ab][\da-f]{3}-[\da-f]{12}$/);
  assert.throws(() => requestId({}), /seguro/);
});
test('double click does not send twice; success clears draft, queries plan and refreshes detail', async () => {
  let finish, sent = 0, refreshed = 0;
  const { session, gets } = await ready(async () => { sent++; return new Promise(resolve => { finish = resolve; }); }, async () => { refreshed++; });
  const pending = session.confirm(); await session.confirm(); assert.equal(sent, 1);
  finish({ revision: { id: 8 }, estadoActual: preparationFixture() }); await pending;
  assert.equal(session.getSnapshot().status, 'success'); assert.equal(session.getSnapshot().draft, null);
  assert.equal(gets(), 2); assert.equal(refreshed, 1); await session.confirm(); assert.equal(sent, 1);
});
test('ambiguous timeout freezes exact payload and preserves UUID until retry succeeds', async () => {
  const requests = [];
  const { session, ids } = await ready(async (_, payload) => { requests.push(structuredClone(payload)); if (requests.length === 1) throw new Error('Timeout'); return { revision: { id: 8 }, estadoActual: preparationFixture() }; });
  await session.confirm(); assert.equal(session.getSnapshot().status, 'ambiguous');
  const before = session.getSnapshot().draft;
  session.edit(d => ({ ...d, comentario: 'Changed' })); await session.load(true);
  assert.equal(session.getSnapshot().draft, before);
  await session.confirm(); assert.deepEqual(requests[0], requests[1]); assert.equal(ids(), 1);
});
test('409 preserves draft and versions until explicit discard/reload, without automatic resubmission', async () => {
  let sends = 0;
  const { session, gets } = await ready(async () => { sends++; throw failure(409); });
  const before = session.getSnapshot().draft;
  await session.confirm(); assert.equal(session.getSnapshot().status, 'conflict'); assert.equal(session.getSnapshot().draft, before);
  await session.confirm(); await session.load(); assert.equal(sends, 1); assert.equal(gets(), 1);
  await session.load(true); assert.equal(gets(), 2); assert.equal(session.getSnapshot().draft.confirmado, false); assert.deepEqual(session.getSnapshot().draft.edited, {});
});
test('403 disables revision; validation response keeps editable draft', async () => {
  for (const status of [403, 400]) {
    const { session } = await ready(async () => { throw failure(status); });
    const draft = session.getSnapshot().draft; await session.confirm();
    assert.equal(session.getSnapshot().draft, draft); assert.equal(session.getSnapshot().error, `Error ${status}`);
    session.edit(d => ({ ...d, comentario: 'Correction' }));
    assert.equal(session.getSnapshot().draft.comentario, status === 403 ? 'Motivo' : 'Correction');
  }
});
test('a different confirmed operation uses a new request identifier after validation correction', async () => {
  const ids = [];
  const { session } = await ready(async (_, p) => { ids.push(p.solicitudId); throw failure(400); });
  await session.confirm(); session.edit(d => ({ ...d, comentario: 'Correction', confirmado: true })); await session.confirm();
  assert.notEqual(ids[0], ids[1]);
});
test('a successful POST is not retried when detail refresh fails', async () => {
  let sends = 0;
  const { session } = await ready(async () => { sends++; return { revision: { id: 8 }, estadoActual: preparationFixture() }; }, async () => { throw new Error('Detail unavailable'); });
  await session.confirm(); await session.confirm(); assert.equal(sends, 1);
  assert.equal(session.getSnapshot().status, 'success'); assert.match(session.getSnapshot().warning, /detalle/);
});

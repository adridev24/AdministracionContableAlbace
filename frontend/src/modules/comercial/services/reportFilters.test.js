import test from 'node:test';
import assert from 'node:assert/strict';
import { defaultReportFilters, generalReportFilters, reportQuery, appliedReportFilters, reportCaption } from './reportFilters.js';

test('initial General has empty dates and no query parameters', () => {
  assert.deepEqual(defaultReportFilters(), { alcance: 'General', desde: '', hasta: '', via: 'Todos' });
  assert.equal(reportQuery(), '');
});

test('General discards residual dates and preserves each via', () => {
  for (const via of ['Todos', 'Via1', 'Via2']) {
    const filters = { alcance: 'General', desde: '2026-10-01', hasta: '2026-10-31', via };
    assert.equal(reportQuery(filters), via === 'Todos' ? '' : `via=${via}`);
    assert.deepEqual(generalReportFilters(filters), { ...defaultReportFilters(), via });
  }
});

test('Period requires two valid calendar dates in ascending order', () => {
  for (const [desde, hasta] of [['', '2026-10-31'], ['2026-10-01', ''], ['', ''], ['2026-02-30', '2026-03-01'], ['2026-10-31', '2026-10-01']]) {
    assert.throws(() => reportQuery({ alcance: 'Periodo', desde, hasta }), /fecha/);
  }
  assert.equal(reportQuery({ alcance: 'Periodo', desde: '2026-10-01', hasta: '2026-10-01', via: 'Via1' }), 'desde=2026-10-01&hasta=2026-10-01&via=Via1');
});

test('editing draft filters cannot change the applied caption', () => {
  const draft = { alcance: 'Periodo', desde: '2026-10-01', hasta: '2026-10-31', via: 'Via2' };
  const applied = appliedReportFilters(draft, { alcance: 'Periodo', periodoDesde: '2026-10-01T00:00:00Z', periodoHasta: '2026-10-31T00:00:00Z' });
  draft.desde = '2027-01-01'; draft.via = 'Via1'; draft.alcance = 'General';
  assert.equal(reportCaption(applied), 'Período: 01/10/2026 al 31/10/2026 · Vía 2 · Importes separados por moneda');
  const reset = generalReportFilters(draft);
  assert.equal(reportQuery(reset), 'via=Via1');
  assert.equal(reportCaption(appliedReportFilters(reset, { alcance: 'General', periodoDesde: null, periodoHasta: null })), 'Totales generales · Vía 1 · Importes separados por moneda');
});

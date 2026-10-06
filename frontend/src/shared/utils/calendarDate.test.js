import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import process from 'node:process';
import { calendarDateValue, calendarDateToApi, formatCalendarDate } from './calendarDate.js';

test('preserves the written day in API responses, including explicit offsets', () => {
  for (const value of ['2026-10-03', '2026-10-03T00:00:00Z', '2026-10-03T00:00:00.0000000+14:00']) {
    assert.equal(calendarDateValue(value), '2026-10-03');
    assert.equal(formatCalendarDate(value), '03/10/2026');
  }
});

test('handles absent and invalid values without throwing', () => {
  for (const value of [null, undefined, '', {}, 123, 'invalid', '2026-02-29', '2026-04-31', '0000-01-01', '2026-13-01', '2026-10-03garbage', '2026-10-03T25:00:00Z']) {
    assert.equal(calendarDateValue(value), '');
    assert.equal(formatCalendarDate(value), '-');
    assert.equal(calendarDateToApi(value), null);
  }
  assert.equal(calendarDateValue('2024-02-29'), '2024-02-29');
});

test('save and edit round trips preserve independent deadlines', () => {
  for (const day of ['2026-10-03', '2026-11-15']) {
    let value = day;
    for (let i = 0; i < 5; i++) {
      const response = JSON.parse(JSON.stringify({ fechaVencimiento: calendarDateToApi(value) }));
      value = calendarDateValue(response.fechaVencimiento);
      assert.equal(value, day);
    }
  }
});

test('Argentina, UTC and Tokyo render the same calendar date', () => {
  const url = new URL('./calendarDate.js', import.meta.url).href;
  for (const TZ of ['America/Argentina/Buenos_Aires', 'UTC', 'Asia/Tokyo']) {
    const result = execFileSync(process.execPath, ['--input-type=module', '-e',
      `import { formatCalendarDate } from ${JSON.stringify(url)}; console.log(formatCalendarDate('2026-10-03T00:00:00Z'));`],
    { env: { ...process.env, TZ }, encoding: 'utf8' });
    assert.equal(result.trim(), '03/10/2026');
  }
});

import test from 'node:test';
import assert from 'node:assert/strict';
import { hasActiveSession } from './authUtils.js';
const token = payload => 'header.' + Buffer.from(JSON.stringify(payload)).toString('base64url') + '.signature';
for (const [name, value, expected] of [
  ['restore existing unexpired session in a new tab', token({ exp: Date.now() / 1000 + 3600 }), true],
  ['expired token returns to login', token({ exp: Date.now() / 1000 - 1 }), false],
  ['missing token returns to login', null, false],
  ['missing expiration does not restore session', token({ name: 'test' }), false]
]) test(name, () => {
  const original = globalThis.localStorage;
  globalThis.localStorage = { getItem: () => value };
  try { assert.equal(hasActiveSession(), expected); } finally { globalThis.localStorage = original; }
});

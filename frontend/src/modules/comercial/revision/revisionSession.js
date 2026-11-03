import { buildRequest, newDraft, preview } from './revisionDraft.js';

export function requestId(crypto = globalThis.crypto) {
  if (crypto?.randomUUID) return crypto.randomUUID();
  if (!crypto?.getRandomValues) throw new Error('Este navegador no dispone de un generador seguro de identificadores.');
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 15) | 64; bytes[8] = (bytes[8] & 63) | 128;
  const hex = [...bytes].map(b => b.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
export function apiError(error) {
  const data = error?.response?.data;
  return data?.error || (data?.errors && Object.values(data.errors).flat().join(' ')) || data?.title || error?.message || 'No se pudo completar la solicitud.';
}
// Shared by React and integration tests: a frozen request survives ambiguous transport failures.
export function createRevisionSession({ api, target, onSuccess = async () => {}, uuid = requestId }) {
  let state = { status: 'idle', draft: null, error: '', result: null, warning: '' };
  let pending = null;
  const listeners = new Set();
  const update = (values) => { state = { ...state, ...values }; listeners.forEach(fn => fn()); };
  const session = {
    getSnapshot: () => state,
    subscribe: (fn) => { listeners.add(fn); return () => listeners.delete(fn); },
    edit: (change) => {
      if (!['ready', 'validation'].includes(state.status) || pending || !state.draft) return;
      update({ draft: change(state.draft), status: 'ready', error: '' });
    },
    load: async (discardConfirmed = false) => {
      if (['loading', 'sending', 'ambiguous', 'forbidden'].includes(state.status)) return;
      if (state.draft && !discardConfirmed) return;
      update({ status: 'loading', error: '' });
      try {
        const data = await api.prepare(target);
        pending = null;
        update({ status: 'ready', draft: newDraft(data), warning: '', result: null });
      } catch (error) {
        update({ status: error?.response?.status === 403 ? 'forbidden' : 'loadError', error: apiError(error) });
      }
    },
    confirm: async () => {
      if (!['ready', 'validation', 'ambiguous'].includes(state.status) || !state.draft) return;
      if (!pending && !preview(state.draft).canConfirm) return;
      try { pending ||= buildRequest(state.draft, uuid()); } catch (error) { update({ status: 'validation', error: error.message }); return; }
      update({ status: 'sending', error: '' });
      let result;
      try { result = await api.confirm(target.viaId, pending); }
      catch (error) {
        const code = error?.response?.status;
        const status = code === 403 || code === 401 ? 'forbidden' : code === 409 ? 'conflict' : code >= 400 && code < 500 && code !== 408 ? 'validation' : 'ambiguous';
        if (status !== 'ambiguous') pending = null;
        update({ status, error: apiError(error) });
        return;
      }
      // A confirmed POST is never retried because a later GET failed.
      pending = null;
      update({ status: 'success', draft: null, result });
      let current = result.estadoActual;
      const warnings = [];
      try { current = await api.prepare(target); } catch { warnings.push('La revisión fue guardada, pero no se pudo volver a consultar el plan.'); }
      update({ result: { ...result, estadoActual: current } });
      try { await onSuccess(result, current); } catch { warnings.push('No se pudo actualizar el detalle del acuerdo. Recargue el detalle.'); }
      update({ warning: warnings.join(' ') });
    }
  };
  return session;
}

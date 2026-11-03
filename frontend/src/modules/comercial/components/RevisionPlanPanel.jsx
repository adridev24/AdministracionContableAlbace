import { useEffect, useRef, useState, useSyncExternalStore } from 'react';
import revisionPlanService from '../services/revisionPlanService.js';
import { createRevisionSession } from '../revision/revisionSession.js';
import { addQuota, canRetire, cents, editAdded, editQuota, formatMoney, isChanged, orderedQuotas, preview,
  quotaValues, removeAdded, restoreRetirements, retireQuota, retirementText, tailQuota } from '../revision/revisionDraft.js';
import { formatCalendarDate } from '../../../shared/utils/calendarDate.js';
import './revisionPlan.css';

export function RevisionBalances({ data, title = 'Situación vigente' }) {
  return <section aria-label={title}>
    <h3>{title} · {data.monedaCodigo}</h3>
    <dl className="revision-metrics">
      {[
        ['MontoOriginal (solo lectura)', data.montoOriginal], ['MontoActual vigente', data.montoActual],
        ['Total vigente calculado', data.totalVigente], ['Total pagado válido', data.totalPagadoValido],
        ['Saldo pendiente', data.saldoPendiente], ['Saldo a favor', data.saldoAFavor]
      ].map(([label, amount]) => <div key={label}><dt>{label}</dt><dd>{formatMoney(cents(amount), data.monedaCodigo)}</dd></div>)}
    </dl>
  </section>;
}

export function RevisionPlanView({ state, session }) {
  const { draft, status, result } = state;
  const disabled = !['ready', 'validation'].includes(status);
  const change = (fn) => session.edit(fn);
  const money = (value) => formatMoney(value, draft?.data.monedaCodigo || result?.estadoActual.monedaCodigo);
  const view = draft ? preview(draft) : null;
  const reload = () => {
    if (!draft || window.confirm('Recargar reemplazará el borrador. Deberá revisar y volver a ingresar sus modificaciones. ¿Continuar?')) session.load(true);
  };
  const renderQuota = (quota) => {
    const values = quotaValues(draft, quota), removed = draft.removed.includes(quota.id), changed = isChanged(draft, quota);
    const last = tailQuota(draft)?.id === quota.id;
    return <article key={quota.id} className={`revision-quota ${removed ? 'revision-removed' : changed ? 'revision-changed' : ''}`} aria-label={`${quota.tipo} ${quota.numero}`}>
      <div className="revision-quota-heading"><strong>{quota.tipo} {quota.numero}</strong><span>{quota.estado} · {quota.tieneHistoria ? 'Con historial' : 'Sin historial'}</span></div>
      {removed ? <p>{retirementText(quota)} (pendiente de confirmar).</p> : <>
        <div className="revision-fields">
          <label>Importe · {draft.data.monedaCodigo}
            <input aria-label={`Importe ${quota.tipo} ${quota.numero}`} inputMode="decimal" value={values.importe}
              disabled={disabled || !quota.puedeModificar} className={cents(values.importe) !== cents(quota.importe) ? 'revision-field-changed' : ''}
              onChange={e => change(d => editQuota(d, quota.id, 'importe', e.target.value))} />
          </label>
          <label>Vencimiento
            <input aria-label={`Vencimiento ${quota.tipo} ${quota.numero}`} type="date" value={values.vencimiento} min="1900-01-01"
              disabled={disabled || !quota.puedeModificar} className={values.vencimiento !== quota.vencimiento.slice(0, 10) ? 'revision-field-changed' : ''}
              onChange={e => change(d => editQuota(d, quota.id, 'vencimiento', e.target.value))} />
          </label>
          {last && quota.tipo === 'Cuota' && <button className="btn-secondary" type="button" disabled={disabled || !canRetire(draft, quota)}
            onClick={() => change(d => retireQuota(d, quota.id))}>Retirar cuota {quota.numero}</button>}
        </div>
        {changed && <p className="revision-note">Modificada · Antes: {money(cents(quota.importe))} · {formatCalendarDate(quota.vencimiento)}</p>}
        {quota.motivoBloqueoModificacion && <p className="revision-note">No se puede modificar: {quota.motivoBloqueoModificacion}</p>}
        {last && !canRetire(draft, quota) && <p className="form-error">Retiro bloqueado: {quota.motivoBloqueoModificacion || quota.motivoBloqueoRetiro}</p>}
      </>}
    </article>;
  };
  return <>
    {['idle', 'loading'].includes(status) && <p role="status">Cargando preparación de la revisión…</p>}
    {state.error && <p className="form-error" role="alert">{state.error}</p>}
    {status === 'forbidden' && <p role="alert">No posee permisos para revisar este plan. La revisión está deshabilitada.</p>}
    {status === 'conflict' && <div role="alert" className="alert-box">El plan cambió o está siendo revisado. Conservamos su borrador para que pueda consultarlo. No se reenviará automáticamente.
      <button type="button" className="btn-secondary" onClick={reload}>Recargar datos</button></div>}
    {status === 'loadError' && <button type="button" className="btn-secondary" onClick={reload}>Recargar datos</button>}
    {status === 'ambiguous' && <div role="alert" className="alert-box">No se pudo confirmar el resultado. Los cambios quedan protegidos para reintentar exactamente la misma solicitud, sin duplicarla.
      <button type="button" className="btn-primary" onClick={() => session.confirm()}>Reintentar la misma confirmación</button></div>}
    {status === 'success' && <div role="status"><p className="form-success">Revisión {result.revision.id} confirmada. El plan continúa aprobado.</p>
      <RevisionBalances data={result.estadoActual} title="Situación después de la revisión" />
      {state.warning && <p className="form-error">{state.warning}</p>}
    </div>}
    {draft && <>
      <RevisionBalances data={draft.data} />
      <section aria-label="Anticipo separado"><h3>Anticipo</h3>
        <p className="revision-note">Se muestra por separado y no participa del retiro correlativo de cuotas ordinarias.</p>
        {orderedQuotas(draft).filter(c => c.tipo === 'Anticipo').map(renderQuota)}
        {!draft.data.cuotas.some(c => c.tipo === 'Anticipo') && <p>Sin anticipo.</p>}
      </section>
      <section aria-label="Cuotas del plan"><h3>Cuotas y otras obligaciones</h3>
        <p className="revision-note">Número y tipo se conservan. Solo puede retirar la última cuota ordinaria habilitada.</p>
        {orderedQuotas(draft).filter(c => c.tipo !== 'Anticipo').map(renderQuota)}
        {draft.removed.length > 0 && <button type="button" className="btn-secondary" disabled={disabled} onClick={() => change(restoreRetirements)}>Deshacer retiros pendientes</button>}
      </section>
      <section aria-label="Cuotas nuevas"><h3>Agregar cuotas ordinarias</h3>
        {draft.added.map((quota, index) => <article className="revision-quota revision-changed" key={quota.numero}>
          <strong>Nueva cuota {quota.numero} · pendiente de guardar</strong>
          <div className="revision-fields">
            <label>Importe · {draft.data.monedaCodigo}<input aria-label={`Importe nueva cuota ${quota.numero}`} inputMode="decimal" value={quota.importe} disabled={disabled}
              onChange={e => change(d => editAdded(d, index, 'importe', e.target.value))} /></label>
            <label>Vencimiento<input aria-label={`Vencimiento nueva cuota ${quota.numero}`} type="date" min="1900-01-01" value={quota.vencimiento} disabled={disabled}
              onChange={e => change(d => editAdded(d, index, 'vencimiento', e.target.value))} /></label>
            <button type="button" className="btn-secondary" disabled={disabled} onClick={() => change(d => removeAdded(d, index))}>Quitar alta pendiente</button>
          </div>
        </article>)}
        <button type="button" className="btn-secondary" disabled={disabled} onClick={() => change(addQuota)}>Agregar cuota {draft.data.siguienteNumeroCuota + draft.added.length}</button>
        <p className="revision-note">Adicional, Refuerzo y Ajuste mantienen su circuito específico. Al quitar un alta pendiente, solo se recalculan los números previstos de las altas aún no guardadas.</p>
      </section>
      <section className="revision-summary" aria-label="Resumen final de revisión"><h3>Resumen y confirmación</h3>
        <div className="revision-fields">
          <label>Nuevo MontoActual · {draft.data.monedaCodigo}<input aria-label="Nuevo MontoActual" inputMode="decimal" value={draft.nuevoMonto} disabled={disabled}
            onChange={e => change(d => ({ ...d, nuevoMonto: e.target.value, confirmado: false }))} /></label>
          <strong aria-live="polite">{view.matches ? 'Coincide' : 'No coincide'}</strong>
        </div>
        <dl className="revision-metrics">
          {[
            ['MontoActual anterior', view.anterior], ['Nuevo MontoActual', view.nuevo], ['Total de obligaciones resultante', view.total],
            ['Diferencia', view.difference], ['Saldo pendiente estimado', view.pending], ['Saldo a favor estimado', view.credit]
          ].map(([label, amount]) => <div key={label}><dt>{label}</dt><dd>{money(amount)}</dd></div>)}
        </dl>
        <ul>
          {view.modified.map(c => <li key={`m${c.id}`}>Modificar {c.tipo} {c.numero}: {money(cents(c.importe))} → {money(cents(quotaValues(draft, c).importe))}; {formatCalendarDate(c.vencimiento)} → {formatCalendarDate(quotaValues(draft, c).vencimiento)}.</li>)}
          {view.added.map(c => <li key={`a${c.numero}`}>Agregar cuota {c.numero}: {money(cents(c.importe))}; {formatCalendarDate(c.vencimiento)}.</li>)}
          {view.retired.map(c => <li key={`r${c.id}`}>Cuota {c.numero}: {retirementText(c)}.</li>)}
        </ul>
        <label className="revision-comment">Comentario obligatorio<textarea aria-label="Comentario obligatorio" maxLength={2000} value={draft.comentario} disabled={disabled}
          onChange={e => change(d => ({ ...d, comentario: e.target.value, confirmado: false }))} /></label>
        <p>Comentario de la revisión: {draft.comentario.trim() || 'Pendiente'}</p>
        {view.errors.length > 0 && <ul className="revision-note">{view.errors.map(error => <li key={error}>{error}</li>)}</ul>}
        <label className="revision-confirm"><input type="checkbox" checked={draft.confirmado} disabled={disabled || view.errors.length > 0}
          onChange={e => change(d => ({ ...d, confirmado: e.target.checked }))} />Confirmo la revisión del plan aprobado</label>
        <button className="btn-primary" type="button" disabled={disabled || !view.canConfirm} onClick={() => session.confirm()}>
          {status === 'sending' ? 'Confirmando…' : 'Confirmar revisión y mantener aprobado'}
        </button>
      </section>
    </>}
  </>;
}

export default function RevisionPlanPanel({ acuerdoId, viaId, planId, onClose, onSuccess, api = revisionPlanService }) {
  const [session] = useState(() => createRevisionSession({ api, target: { acuerdoId, viaId, planId }, onSuccess }));
  const state = useSyncExternalStore(session.subscribe, session.getSnapshot, session.getSnapshot);
  const dialog = useRef(null);
  useEffect(() => { session.load(); dialog.current?.showModal(); }, [session]);
  const cannotClose = ['sending', 'ambiguous'].includes(state.status);
  const close = () => {
    if (cannotClose) return;
    if (state.draft && (Object.keys(state.draft.edited).length || state.draft.added.length || state.draft.removed.length || state.draft.comentario || cents(state.draft.nuevoMonto) !== cents(state.draft.data.montoActual)) &&
      !window.confirm('Cerrar descartará el borrador de revisión. ¿Continuar?')) return;
    onClose();
  };
  return <dialog ref={dialog} className="revision-dialog" aria-labelledby="revision-title" onCancel={e => { e.preventDefault(); close(); }}>
    <header className="revision-header"><div><h2 id="revision-title">Revisar plan aprobado</h2><p>Acuerdo {acuerdoId} · Plan {planId}</p></div>
      <button type="button" className="btn-secondary" onClick={close} disabled={cannotClose}>Cerrar revisión</button></header>
    <RevisionPlanView state={state} session={session} />
  </dialog>;
}

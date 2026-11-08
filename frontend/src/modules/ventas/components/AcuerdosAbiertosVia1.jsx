import NumericInput from '../../../shared/components/NumericInput';
import { Link } from 'react-router-dom';
import { formatCalendarDate } from '../../../shared/utils/calendarDate';

const money = (amount, currency) => currency + ' ' + Number(amount || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export default function AcuerdosAbiertosVia1({ consulta, editable = false, aplicaciones = {}, onChange, moneda, onMonedaChange, neto = 0, referencia = 0 }) {
  const { rows, loading, error, mensaje, refresh } = consulta;
  const conversion = moneda === 'USD';
  const objetivo = conversion ? (Number(referencia) > 0 ? Math.round(neto / Number(referencia) * 100) / 100 : 0) : neto;
  const total = Object.values(aplicaciones).reduce((s, x) => s + Number(x || 0), 0);
  if (!loading && !error && rows.length === 0) return null;
  return <fieldset className="factura-importes">
    <legend>Acuerdos abiertos · Vía 1</legend>
    {loading ? <p role="status">Consultando acuerdos abiertos...</p>
      : error ? <p className="form-error" role="alert">{error}</p>
      : <>
        {mensaje && <p className="form-error" role="alert">{mensaje}</p>}
        {editable && <>
          <label>Moneda del acuerdo <select value={moneda} onChange={e => onMonedaChange(e.target.value)}>
            {[...new Set(rows.map(a => a.monedaCodigo))].map(m => <option key={m}>{m}</option>)}
          </select></label>
          <p>Distribuí el neto entre los hitos. Neto a imputar: {money(objetivo, moneda)} · Imputado: {money(total, moneda)} · Pendiente: {money(objetivo - total, moneda)}.</p>
          {conversion && !(Number(referencia) > 0) && <p role="alert">Completá la cotización de referencia para imputar a dólares.</p>}
        </>}
        {rows.filter(a => !editable || a.monedaCodigo === moneda).map(acuerdo => <section key={acuerdo.viaId}>
          <h4>Acuerdo {acuerdo.numeroAcuerdo} · {acuerdo.monedaCodigo}</h4>
          <p>Monto del acuerdo: <strong>{money(acuerdo.montoActual, acuerdo.monedaCodigo)}</strong> · Disponible del acuerdo: {money(acuerdo.saldoFacturable, acuerdo.monedaCodigo)} · Estado: {acuerdo.estado}</p>
          {acuerdo.hitos.length ? <div className="responsive-table-wrapper"><table className="data-table">
            <thead><tr><th>Hito</th><th>Fecha de referencia</th><th>Importe previsto</th><th>Facturado</th><th>Reservado</th><th>Disponible</th>{editable && <th>Aplicar a esta factura</th>}</tr></thead>
            <tbody>{acuerdo.hitos.map(hito => <tr key={hito.id}>
              <td>{hito.descripcion}</td><td>{formatCalendarDate(hito.fechaReferencia)}</td>
              <td>{money(hito.importeEstimado, acuerdo.monedaCodigo)}</td>
              <td>{money(hito.facturado, acuerdo.monedaCodigo)}</td><td>{money(hito.reservado, acuerdo.monedaCodigo)}</td><td>{money(hito.saldoDisponible, acuerdo.monedaCodigo)}</td>
              {editable && <td><NumericInput aria-label={'Importe para hito ' + hito.descripcion} min="0" max={hito.saldoDisponible} step="0.01"
                disabled={conversion && !(Number(referencia) > 0)} value={aplicaciones[hito.id] ?? ''} onChange={e => onChange(hito.id, e.target.value)} /></td>}
            </tr>)}</tbody>
          </table></div> : <p className="field-help">Debe crear un hito en este acuerdo antes de poder guardar la factura.</p>}
          <Link className="btn-secondary" to={'/comercial/' + acuerdo.acuerdoId + '?via=' + acuerdo.viaId + '#hitos'} target="_blank" rel="noopener noreferrer">
            {acuerdo.hitos.length ? 'Ver o agregar hitos' : 'Crear hito'} (nueva pestaña)
          </Link>
        </section>)}
      </>}
    <button type="button" className="btn-secondary" disabled={loading} onClick={refresh}>Actualizar acuerdos e hitos</button>
  </fieldset>;
}

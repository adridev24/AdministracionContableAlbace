import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import acuerdosService from '../services/acuerdosService';
import { situacionHitos } from './situacionHitos';
import { formatCalendarDate } from '../../../shared/utils/calendarDate';

const currency = (n, moneda) => `${moneda} ${Number(n || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

function Detalle({ acuerdoId, via, hito, onClose }) {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    acuerdosService.getDetalleFacturacionHito(acuerdoId, via.id, hito.id)
      .then(rows => { if (active) setData(rows); })
      .catch(() => { if (active) setError('No se pudo cargar el detalle. Cerrá y volvé a intentar.'); });
    return () => { active = false; };
  }, [acuerdoId, via.id, hito.id]);
  const money = n => currency(n, via.monedaCodigo);
  return <div className="modal-backdrop"><div className="modal-card situacion-modal" role="dialog" aria-modal="true" aria-label={`Facturación y cobranza: ${hito.descripcion}`}>
    <div className="modal-header"><h2>{hito.descripcion}</h2><button type="button" className="modal-close" onClick={onClose} aria-label="Cerrar">×</button></div>
    <div className="modal-body">
      <p>La cobranza se registra en la factura. Los importes del hito representan su parte neta, sin impuestos.</p>
      {error && <p role="alert" className="form-error">{error}</p>}
      {!data && !error && <p>Cargando detalle...</p>}
      {data?.length === 0 && <p>No hay facturas vinculadas a este hito.</p>}
      {data?.map(f => <section key={f.ventaId}>
        <h3><Link to={`/ventas/${f.ventaId}`} target="_blank" rel="noopener noreferrer">Factura {f.comprobante}</Link> · {f.estado}</h3>
        <p>{formatCalendarDate(f.fecha)} · Total con impuestos: {currency(f.totalFactura, f.monedaFactura)} · Neto imputado al hito: {money(f.netoHito)} · Cobrado del hito: {money(f.cobradoHito)}</p>
        {f.estado === 'Borrador' && <p>Esta factura reserva saldo; todavía no cuenta como facturada.</p>}
        {f.cobranzas.length ? <div className="table-wrapper"><table className="data-table">
          <thead><tr><th>Cobranza</th><th>Fecha</th><th>Estado</th><th>Aplicado a la factura (con impuestos)</th><th>Cheques rechazados</th><th>Neto cobrado del hito</th></tr></thead>
          <tbody>{f.cobranzas.map((c, i) => <tr key={`${c.cobranzaId}-${i}`}><td>#{c.cobranzaId}</td><td>{formatCalendarDate(c.fecha)}</td><td>{c.estado}</td><td>{currency(c.aplicadoFactura, f.monedaFactura)}</td><td>{currency(c.rechazado, f.monedaFactura)}</td><td>{money(c.netoHito)}</td></tr>)}</tbody>
        </table></div> : <p>Sin cobranzas aplicadas a esta factura.</p>}
      </section>)}
      <p>Los borradores, las cobranzas anuladas y los importes de cheques rechazados no suman al cobrado. Si una factura tiene varios hitos, el neto cobrado se distribuye en el orden guardado al imputarla.</p>
    </div>
  </div></div>;
}

export default function SituacionHitosPanel({ acuerdoId, via, onUpdated }) {
  const [consulta, setConsulta] = useState(null);
  const [error, setError] = useState('');
  const [refresh, setRefresh] = useState(0);
  const [selected, setSelected] = useState(null);
  const viaId = via.id;
  useEffect(() => {
    let active = true;
    const load = () => acuerdosService.getAcuerdoDetalle(acuerdoId).then(data => {
      if (active) { onUpdated(data); setConsulta(true); setError(''); }
    }).catch(() => { if (active) setError('No se pudo actualizar la situación de los hitos. Intentá actualizar nuevamente.'); });
    load();
    window.addEventListener('focus', load);
    return () => { active = false; window.removeEventListener('focus', load); };
  }, [acuerdoId, viaId, refresh, onUpdated]);
  const s = consulta ? situacionHitos(via) : null;
  const money = n => currency(n, via.monedaCodigo);
  return <div className="situacion-via1">
    <p>Importes netos en {via.monedaCodigo}. La cobranza se aplica a las facturas y se refleja en sus hitos.</p>
    <button type="button" className="btn-secondary" onClick={() => { setConsulta(null); setSelected(null); setRefresh(n => n + 1); }}>Actualizar situación</button>
    {error && <p role="alert" className="form-error">{error}</p>}
    {!s && !error && <p>Cargando situación de hitos...</p>}
    {s && !error && <>
      <div className="summary-grid situacion-summary">{[['Acordado', s.acordado], ['Facturado', s.facturado], ['Pendiente de facturar', s.pendienteFacturar], ['Cobrado', s.cobrado], ['Facturado pendiente de cobro', s.pendienteCobro], ['Pendiente total', s.pendienteTotal]].map(([label, value]) => <div className="summary-card" key={label}><span>{label}</span><strong>{money(value)}</strong></div>)}</div>
      {s.reservado > 0 && <p className="alert-box">Reservado en borradores: {money(s.reservado)}. Reduce el disponible para nuevas facturas, pero no se cuenta como facturado.</p>}
      {s.hitos.length ? <div className="table-wrapper"><table className="data-table">
        <thead><tr><th>Hito</th><th>Fecha</th><th>Monto</th><th>Facturado</th><th>Pend. facturar</th><th>Cobrado</th><th>Pend. cobro</th><th>Facturación</th><th>Cobranza</th><th>Detalle</th></tr></thead>
        <tbody>{s.hitos.map(h => <tr key={h.id}><td>{h.descripcion}</td><td>{formatCalendarDate(h.fechaReferencia)}</td><td>{money(h.previsto)}</td><td>{money(h.facturado)}</td><td>{money(h.pendienteFacturar)}</td><td>{money(h.cobrado)}</td><td>{money(h.pendienteCobro)}</td><td>{h.estadoFacturacion}</td><td>{h.estadoCobranza}</td><td><button type="button" className="btn-secondary btn-small" onClick={() => setSelected(h)}>Ver detalle</button></td></tr>)}</tbody>
      </table></div> : <p>Creá un hito para poder imputar facturas a este acuerdo abierto.</p>}
    </>}
    {selected && <Detalle key={`${via.id}-${selected.id}`} acuerdoId={acuerdoId} via={via} hito={selected} onClose={() => setSelected(null)} />}
  </div>;
}

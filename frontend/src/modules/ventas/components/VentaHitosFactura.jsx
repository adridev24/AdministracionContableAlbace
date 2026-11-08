import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import useAcuerdosAbiertosVia1 from '../hooks/useAcuerdosAbiertosVia1';
import AcuerdosAbiertosVia1 from './AcuerdosAbiertosVia1';
import ventasService from '../services/ventasService';

export default function VentaHitosFactura({ venta, readOnly, onSaved, onImputacionChange }) {
  const consulta = useAcuerdosAbiertosVia1(venta.clienteExternoId, venta.obraExternaId, venta.monedaCodigo, venta.id);
  const [links, setLinks] = useState([]);
  const [aplicaciones, setAplicaciones] = useState({});
  const [moneda, setMoneda] = useState(venta.monedaCodigo);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [loaded, setLoaded] = useState(false);
  useEffect(() => {
    let active = true;
    ventasService.getVinculacionesHitos(venta.id).then(items => {
      if (!active) return;
      onImputacionChange?.(items.length > 0);
      setLinks(items); setAplicaciones(Object.fromEntries(items.map(x => [x.hitoId, x.importe])));
      if (items.length) setMoneda(items[0].monedaCodigo);
      setLoaded(true);
    }).catch(() => { if (active) setError('No se pudieron cargar las imputaciones a hitos. Recargá la factura.'); });
    return () => { active = false; };
  }, [venta.id, onImputacionChange]);
  const currency = consulta.rows.some(a => a.monedaCodigo === moneda) ? moneda : consulta.rows[0]?.monedaCodigo || moneda;
  const neto = venta.fiscalSimple?.imputaNetoPlan ? Number(venta.netoGravado || 0) : Number(venta.totalAntesPercepciones || 0) - Number(venta.totalIva || 0);
  async function save(remove = false) {
    setSaving(true); setError('');
    try {
      const items = remove ? [] : Object.entries(aplicaciones).filter(([,v]) => Number(v) > 0).map(([id,v]) => ({ hitoId: Number(id), importe: Number(v) }));
      if (!remove && items.length === 0) throw new Error('Ingresá un importe para al menos un hito.');
      const saved = await ventasService.updateVinculacionesHitos(venta.id, items);
      onImputacionChange?.(saved.length > 0);
      setLinks(saved); setAplicaciones(Object.fromEntries(saved.map(x => [x.hitoId, x.importe]))); consulta.refresh();
      await onSaved();
    } catch (e) { setError(e.response?.data?.error || e.message || 'No se pudieron guardar las imputaciones.'); }
    finally { setSaving(false); }
  }
  if (loaded && links.length === 0 && !consulta.loading && !consulta.error && consulta.rows.length === 0) return null;
  return <section>
    {error && <p role="alert" className="form-error">{error}</p>}
    {links.length > 0 && <><h3>Imputación de la factura a hitos</h3><table className="data-table">
      <thead><tr><th>Acuerdo</th><th>Hito</th><th>Neto imputado</th></tr></thead>
      <tbody>{links.map(x => <tr key={x.hitoId}><td><Link to={'/comercial/' + x.acuerdoId + '?via=' + x.viaId + '#hitos'}>{x.numeroAcuerdo}</Link></td><td>{x.descripcion}</td><td>{x.monedaCodigo} {Number(x.importe).toFixed(2)}</td></tr>)}</tbody>
    </table></>}
    {!readOnly && <>
      <AcuerdosAbiertosVia1 consulta={consulta} editable={loaded && !saving} aplicaciones={aplicaciones}
        onChange={(id,value) => setAplicaciones(prev => ({...prev, [id]:value}))} moneda={currency}
        onMonedaChange={value => { setMoneda(value); setAplicaciones({}); }} neto={neto} referencia={venta.fiscalSimple?.cotizacionDolarReferencia} />
      {consulta.rows.length > 0 && <button type="button" className="btn-primary" disabled={!loaded || saving || consulta.bloqueado} onClick={() => save()}>Guardar imputación a hitos</button>}
      {links.length > 0 && <button type="button" className="btn-secondary" disabled={saving} onClick={() => save(true)}>Quitar imputación a hitos</button>}
    </>}
  </section>;
}

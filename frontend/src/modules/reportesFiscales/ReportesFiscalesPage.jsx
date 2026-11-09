import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../shared/components/SectionCard';
import { fiscales, descargarFiscal, mensajeError } from './reportesFiscalesService';
import { EmpresaForm, VentaFiscalForm, RetencionFiscalForm } from './FormulariosFiscales';
import './reportesFiscales.css';

const dinero = value => Number(value).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fecha = value => value?.slice(0, 10) || '—';
const mesActual = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`; };
const pestañas = [['empresa', 'Empresa'], ['libro', 'Libro IVA Ventas'], ['retenciones', 'Retenciones sufridas'], ['conciliacion', 'Conciliación'], ['historial', 'Exportaciones']];

function Conciliacion({ reporte, busy, ejecutar, descargar }) {
  const [tipo, setTipo] = useState('ventas');
  const [cuit, setCuit] = useState('');
  const [resultado, setResultado] = useState(null);
  async function enviar(e) {
    e.preventDefault();
    const datos = new FormData(e.currentTarget);
    datos.set('periodo', reporte.periodo); datos.set('cuitEmpresa', cuit);
    setResultado(null);
    await ejecutar(async () => setResultado(await fiscales.conciliar(tipo, datos)));
  }
  return <>
    <h2>Comparar con archivos de referencia</h2>
    <p>La comparación no incorpora operaciones ni cambia ventas o cobranzas. Confirme que los archivos corresponden a la empresa y al período seleccionado.</p>
    <form onSubmit={enviar} key={tipo}>
      <fieldset disabled={busy} className="fiscal-form-grid">
        <label className="fiscal-campo">Comparación<select value={tipo} onChange={e => { setTipo(e.target.value); setResultado(null); }}><option value="ventas">Ventas</option><option value="retenciones">Retenciones sufridas</option></select></label>
        <label className="fiscal-campo">CUIT de la empresa de los archivos<input required value={cuit} onChange={e => { setCuit(e.target.value); setResultado(null); }} placeholder={reporte.empresa?.cuit || ''} /></label>
        {tipo === 'ventas' ? <>
          <label className="fiscal-campo">Comprobantes de referencia (.txt)<input required name="comprobantes" type="file" accept=".txt" onChange={() => setResultado(null)} /></label>
          <label className="fiscal-campo">Alícuotas de referencia (.txt)<input required name="alicuotas" type="file" accept=".txt" onChange={() => setResultado(null)} /></label>
        </> : <label className="fiscal-campo">Referencia de certificados (.csv)<input required name="archivo" type="file" accept=".csv" onChange={() => setResultado(null)} /></label>}
      </fieldset>
      {tipo === 'ventas' ? <p className="hint-text">Archivos de ventas del diseño ARCA: comprobantes de 266 caracteres y alícuotas de 62, importes en moneda original. Hasta 10 MB por archivo. No admite rangos diarios agrupados.</p>
        : <p className="hint-text">Use la plantilla del módulo para normalizar la referencia del período de cómputo: CSV UTF-8, separado por punto y coma; fecha AAAA-MM-DD, importe en pesos sin separador de miles. Esta plantilla no es un archivo para presentar ante ARCA.</p>}
      <div className="fiscal-actions"><button disabled={busy} className="btn-primary">Comparar archivos</button>{tipo === 'retenciones' && <button disabled={busy} type="button" className="btn-secondary" onClick={() => descargar('plantilla-retenciones.csv', 'Plantilla_retenciones.csv')}>Descargar plantilla</button>}</div>
    </form>
    {resultado && <div aria-live="polite"><div className="fiscal-summary"><strong>{resultado.registrosSistema} en sistema</strong><strong>{resultado.registrosReferencia} en referencia</strong><strong>{resultado.coincidentes} coincidentes</strong><strong>{resultado.diferencias.length} observaciones</strong></div>
      {resultado.diferencias.length ? <div className="responsive-table-wrapper"><table className="data-table"><thead><tr><th>Comprobante / certificado</th><th>Resultado</th><th>Detalle</th></tr></thead><tbody>{resultado.diferencias.map((d, i) => <tr key={i}><td>{d.clave}</td><td>{d.estado}</td><td>{d.detalle}</td></tr>)}</tbody></table></div> : <p>Sin diferencias en los registros comparados. Revise también los controles del período.</p>}
    </div>}
  </>;
}

export default function ReportesFiscalesPage() {
  const [tab, setTab] = useState('libro');
  const [periodo, setPeriodo] = useState(mesActual);
  const [empresa, setEmpresa] = useState(null);
  const [tipos, setTipos] = useState([]);
  const [reporte, setReporte] = useState(null);
  const [historial, setHistorial] = useState([]);
  const [busy, setBusy] = useState(false);
  const [iniciando, setIniciando] = useState(true);
  const [error, setError] = useState('');
  const [aviso, setAviso] = useState('');
  const [editor, setEditor] = useState(null);
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    let vigente = true;
    Promise.all([fiscales.empresa(), fiscales.catalogos()]).then(([e, c]) => {
      if (vigente) { setEmpresa(e); setTipos(c.tipos); }
    }).catch(async err => { const texto = await mensajeError(err); if (vigente) setError(texto); })
      .finally(() => { if (vigente) setIniciando(false); });
    return () => { vigente = false; };
  }, []);
  const bloqueado = busy || iniciando;
  async function ejecutar(accion) {
    setBusy(true); setError(''); setAviso('');
    try { await accion(); } catch (err) { setError(await mensajeError(err)); } finally { setBusy(false); }
  }
  async function cargar() {
    const [r, h] = await Promise.all([fiscales.consultar(periodo), fiscales.historial(periodo)]);
    setReporte(r); setEmpresa(r.empresa); setHistorial(h); setEditor(null); setRevision(v => v + 1);
  }
  const descargar = (ruta, nombre) => ejecutar(() => descargarFiscal(ruta, nombre, periodo));
  const erroresVentas = reporte?.observaciones.filter(o => o.nivel === 'Error' && o.origen !== 'Retencion').length || 0;
  const netoPesos = reporte?.netoPesos || 0;
  const ivaPesos = reporte?.ivaPesos || 0;
  const totalesRetencion = reporte?.retencionesPorImpuesto || {};

  return <div className="page-container fiscal-page">
    <div className="page-header"><div><h1>Reportes contables e impositivos</h1><p className="page-subtitle">Ventas, IVA y retenciones sufridas · {empresa?.razonSocial || 'Configure los datos de la empresa'}</p>{empresa && <p className="hint-text">CUIT {empresa.cuit} · {empresa.condicionIva}</p>}</div><Link className="btn-secondary fiscal-no-print" to="/">Principal</Link></div>
    <nav className="fiscal-tabs" aria-label="Secciones de reportes fiscales">{pestañas.map(([id, titulo]) => <button key={id} type="button" disabled={bloqueado} aria-current={tab === id ? 'page' : undefined} onClick={() => { setTab(id); setEditor(null); }}>{titulo}</button>)}</nav>
    <p className="fiscal-scope">Alcance: ventas y retenciones de cobranzas confirmadas. Compras se incorporará en una etapa posterior. Estos reportes no constituyen una liquidación completa ni una presentación de IVA.</p>
    {tab !== 'empresa' && <form className="fiscal-periodo fiscal-no-print" onSubmit={e => { e.preventDefault(); ejecutar(cargar); }}>
      <label htmlFor="fiscal-periodo">Período fiscal<input id="fiscal-periodo" type="month" required min="2000-01" value={periodo} disabled={bloqueado} onChange={e => { setPeriodo(e.target.value); setReporte(null); setHistorial([]); setEditor(null); setAviso(''); }} /></label>
      <button className="btn-primary" disabled={bloqueado}>{busy ? 'Procesando…' : 'Consultar período'}</button>
    </form>}
    {error && <p className="form-error" role="alert">{error}</p>}{aviso && <p role="status" className="fiscal-success">{aviso}</p>}{iniciando && <p role="status">Cargando datos fiscales…</p>}
    {tab === 'empresa' && !iniciando && <SectionCard><EmpresaForm key={empresa?.actualizadoEl || 'nueva'} empresa={empresa} busy={bloqueado} guardar={datos => ejecutar(async () => {
      const guardada = await fiscales.guardarEmpresa({ ...datos, cuit: datos.cuit.replace(/[-\s]/g, '') }); setEmpresa(guardada); setReporte(null); setAviso('Datos de la empresa guardados.');
    })} /></SectionCard>}
    {tab !== 'empresa' && !reporte && !iniciando && <SectionCard><p>Seleccione un período y consulte para ver los reportes y sus controles.</p></SectionCard>}
    {reporte && tab === 'libro' && <SectionCard title={`Libro IVA Ventas · ${periodo}`}>
      <div className="fiscal-summary"><strong>{reporte.ventas.length} comprobantes</strong><span>Neto gravado ARS <b>{dinero(netoPesos)}</b></span><span>IVA débito ARS <b>{dinero(ivaPesos)}</b></span><span>{erroresVentas} errores para exportación</span></div>
      <div className="fiscal-actions fiscal-no-print">
        <button disabled={bloqueado} className="btn-secondary" onClick={() => descargar('libro.csv', `Libro_IVA_Ventas_${periodo}.csv`)}>Descargar CSV para Excel</button>
        <button disabled={bloqueado} className="btn-secondary" onClick={() => window.print()}>Imprimir / PDF</button>
        <button disabled={bloqueado || !reporte.puedeExportar} className="btn-primary" onClick={() => ejecutar(async () => {
          await descargarFiscal('exportaciones', `IVA_Ventas_${empresa.cuit}_${periodo}.zip`, periodo, true);
          setHistorial(await fiscales.historial(periodo)); setAviso('Exportación generada y respaldada. Extraiga el ZIP y cargue los dos TXT en Portal IVA con importes en moneda original.');
        })}>Generar archivos IVA Simple</button>
      </div>
      <p className="hint-text">Importes de cada fila en moneda original; notas de crédito con signo negativo en el libro. Los TXT utilizan importes positivos y el tipo de comprobante. La generación vuelve a validar los datos actuales.</p>
      {editor?.tipo === 'venta' && <VentaFiscalForm key={editor.fila.id} venta={editor.fila} tipos={tipos} busy={bloqueado} cancelar={() => setEditor(null)} guardar={(id, datos) => ejecutar(async () => { await fiscales.guardarVenta(id, datos); await cargar(); setAviso('Datos fiscales confirmados.'); })} />}
      {reporte.ventas.length ? <div className="responsive-table-wrapper"><table className="data-table fiscal-table"><thead><tr>{['Fecha', 'Comprobante', 'Cliente / documento', 'Moneda / cambio', 'Neto', 'IVA', 'Exento', 'No gravado', 'IIBB', 'Total', 'Alícuotas', 'Datos fiscales'].map(c => <th key={c}>{c}</th>)}</tr></thead>
        <tbody>{reporte.ventas.map(v => <tr key={v.id}><td>{fecha(v.fecha)}</td><td><Link to={`/ventas/${v.id}`}>{v.tipo}<br />{String(v.puntoVenta).padStart(5, '0')}-{String(v.numero).padStart(8, '0')}</Link></td><td>{v.fiscal.razonSocial || `Cliente ${v.clienteId}`}<br />{v.fiscal.documento || 'Sin documento'}</td><td>{v.moneda}<br />{v.cotizacion}</td>{['neto', 'iva', 'exento', 'noGravado', 'percepciones', 'total'].map(k => <td className="fiscal-number" key={k}>{dinero(v[k] * v.signo)}</td>)}<td>{v.alicuotas.map(a => <div key={a.porcentaje}>{a.porcentaje}% · {dinero(a.neto * v.signo)} / {dinero(a.iva * v.signo)}</div>)}</td><td><span>{v.datosConfirmados ? 'Confirmados' : 'Pendientes'}</span><button disabled={bloqueado} className="btn-secondary fiscal-no-print" onClick={() => setEditor({ tipo: 'venta', fila: v })}>Revisar</button></td></tr>)}</tbody></table></div> : <p>Sin ventas confirmadas en el período. No se generan archivos vacíos ni se declara “sin movimiento” automáticamente.</p>}
    </SectionCard>}
    {reporte && tab === 'retenciones' && <SectionCard title={`Retenciones sufridas · ${periodo}`}>
      <p>Se muestran las retenciones cobradas en el mes y las asignadas a este período de cómputo. Los totales computables solo incluyen certificados asignados a {periodo}; revise los errores antes de utilizarlos.</p>
      <div className="fiscal-summary">{Object.entries(totalesRetencion).map(([impuesto, total]) => <span key={impuesto}>{impuesto} ARS <b>{dinero(total)}</b></span>)}</div>
      <div className="fiscal-actions fiscal-no-print"><button className="btn-secondary" disabled={bloqueado} onClick={() => descargar('retenciones.csv', `Retenciones_${periodo}.csv`)}>Descargar CSV para Excel</button><button className="btn-secondary" disabled={bloqueado} onClick={() => window.print()}>Imprimir / PDF</button></div>
      {editor?.tipo === 'retencion' && <RetencionFiscalForm key={editor.fila.id} retencion={editor.fila} periodo={periodo} busy={bloqueado} cancelar={() => setEditor(null)} guardar={(id, datos) => ejecutar(async () => { await fiscales.guardarRetencion(id, { ...datos, cuitAgente: datos.cuitAgente.replace(/[-\s]/g, '') }); await cargar(); setAviso('Certificado guardado.'); })} />}
      {reporte.retenciones.length ? <div className="responsive-table-wrapper"><table className="data-table"><thead><tr>{['Cobranza', 'Impuesto', 'Agente', 'Certificado / régimen', 'Fecha certificado', 'Período cómputo', 'Moneda', 'Importe', 'Importe ARS', 'Datos fiscales'].map(c => <th key={c}>{c}</th>)}</tr></thead><tbody>{reporte.retenciones.map(r => <tr key={r.id}><td>#{r.cobranzaId}<br />{fecha(r.fechaCobranza)}</td><td>{r.impuesto}</td><td>{r.fiscal?.razonSocialAgente || `Cliente ${r.clienteId}`}<br />{r.fiscal?.cuitAgente}</td><td>{r.fiscal?.certificado || 'Pendiente'}<br />{r.fiscal?.regimen} {r.fiscal?.jurisdiccion}</td><td>{fecha(r.fiscal?.fechaRetencion)}</td><td>{r.fiscal?.periodoComputo || 'Sin asignar'}</td><td>{r.moneda}</td><td className="fiscal-number">{dinero(r.importe)}</td><td className="fiscal-number">{dinero(r.importe * r.cotizacion)}</td><td><button disabled={bloqueado} className="btn-secondary fiscal-no-print" onClick={() => setEditor({ tipo: 'retencion', fila: r })}>Completar / revisar</button></td></tr>)}</tbody></table></div> : <p>Sin retenciones sufridas para este período.</p>}
    </SectionCard>}
    {reporte && tab === 'conciliacion' && <SectionCard><Conciliacion key={`${periodo}-${revision}`} reporte={reporte} busy={bloqueado} ejecutar={ejecutar} descargar={descargar} /></SectionCard>}
    {reporte && tab === 'historial' && <SectionCard title="Exportaciones generadas"><p>Cada descarga conserva los archivos originales y el respaldo de la empresa y las operaciones. Generar una nueva exportación crea otra versión.</p>{historial.length ? <div className="responsive-table-wrapper"><table className="data-table"><thead><tr><th>Versión</th><th>Generada</th><th>Usuario</th><th>Comprobantes</th><th>Archivo</th></tr></thead><tbody>{historial.map(h => <tr key={h.id}><td>#{h.id}</td><td>{new Date(h.generadoEl).toLocaleString('es-AR')}</td><td>{h.generadoPor}</td><td>{h.cantidadComprobantes}</td><td><button disabled={bloqueado} className="btn-secondary" onClick={() => descargar(`exportaciones/${h.id}`, h.nombreArchivo)}>Descargar respaldo</button><details><summary>Huella SHA-256</summary><code className="fiscal-hash">{h.sha256}</code></details></td></tr>)}</tbody></table></div> : <p>Todavía no se generaron exportaciones para el período.</p>}</SectionCard>}
    {reporte && tab !== 'empresa' && <SectionCard title="Controles del período" description="Los errores de ventas y empresa bloquean los TXT. Los de retenciones se revisan por separado.">
      {reporte.observaciones.length ? <ul className="fiscal-observaciones">{reporte.observaciones.map((o, i) => <li key={i} className={o.nivel === 'Error' ? 'fiscal-error' : ''}><b>{o.nivel} · {o.origen}{o.id ? ` #${o.id}` : ''}:</b> {o.mensaje}</li>)}</ul> : <p>No se detectaron inconsistencias internas. La comparación con ARCA se realiza en Conciliación.</p>}
    </SectionCard>}
  </div>;
}

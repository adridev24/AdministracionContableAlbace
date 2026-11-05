import { todayArgentina } from '../../../shared/utils/calendarDate';
import NumericInput from '../../../shared/components/NumericInput';
import AlicuotaIibbSelect from './AlicuotaIibbSelect';
import { useEffect, useMemo, useRef, useState } from 'react';
import ClienteObraSelector from '../../comercial/components/ClienteObraSelector';
import ventasService from '../services/ventasService';

const initialForm = {
  tipoComprobanteVentaId: '',
  puntoVentaComprobanteId: '',
  clienteExternoId: '',
  obraExternoId: '',
  fechaComprobante: todayArgentina(),
  puntoVenta: '',
  numeroComprobante: '',
  monedaCodigo: 'ARS',
  cotizacion: '1',
  observaciones: '',
};

const toDateInput = (value) => {
  if (!value) return todayArgentina();
  return new Date(value).toISOString().slice(0, 10);
};

const buildInitialForm = (selectedVenta) => {
  if (!selectedVenta) return { ...initialForm, fechaComprobante: todayArgentina() };

  return {
    tipoComprobanteVentaId: String(selectedVenta.tipoComprobanteVentaId || ''),
    puntoVentaComprobanteId: String(selectedVenta.puntoVentaComprobanteId || ''),
    clienteExternoId: String(selectedVenta.clienteExternoId || ''),
    obraExternoId: String(selectedVenta.obraExternaId || ''),
    fechaComprobante: toDateInput(selectedVenta.fechaComprobante),
    puntoVenta: String(selectedVenta.puntoVenta || ''),
    numeroComprobante: String(selectedVenta.numeroComprobante).padStart(8, '0'),
    monedaCodigo: selectedVenta.monedaCodigo || 'ARS',
    cotizacion: String(selectedVenta.cotizacion || 1),
    observaciones: selectedVenta.observaciones || '',
  };
};

const VentaHeaderForm = ({ tiposComprobante, selectedVenta, saving, onCancel, onSubmit }) => {
  const [form, setForm] = useState(() => buildInitialForm(selectedVenta));
  const [puntosVenta, setPuntosVenta] = useState([]);
  const [loadingPuntos, setLoadingPuntos] = useState(false);
  const [puntosError, setPuntosError] = useState('');
  const requestRef = useRef(0);
  const [referencia, setReferencia] = useState('');
  const [monedaPlan, setMonedaPlan] = useState('ARS');
  const [regimenes, setRegimenes] = useState([]);
  const [regimenId, setRegimenId] = useState('');
  useEffect(() => {
    if (!selectedVenta) ventasService.getPercepcionesIibb({ soloActivos: true })
      .then(setRegimenes)
      .catch(() => setErrorFiscal('No se pudieron cargar los regímenes de IIBB.'));
  }, [selectedVenta]);
  const [tasas, setTasas] = useState([]);
  const [lineas, setLineas] = useState([{alicuotaId:'', neto:''}]);
  const [otros, setOtros] = useState({noGravado:'', exento:'', devolucionCheques:''});
  const [obligaciones, setObligaciones] = useState([]);
  const [loadingObligaciones, setLoadingObligaciones] = useState(false);
  const [errorObligaciones, setErrorObligaciones] = useState('');
  const [aplicado, setAplicado] = useState({});
  const [errorFiscal, setErrorFiscal] = useState('');
  useEffect(() => {
    if (!selectedVenta) ventasService.getAlicuotasIva({soloActivos:true}).then(rows => setTasas(rows.filter(r => r.tipoTratamiento === 'Gravado'))).catch(() => setErrorFiscal('No se pudieron cargar las alícuotas.'));
  }, [selectedVenta]);
  useEffect(() => {
    let actual = true;
    setObligaciones([]); setAplicado({});
    setErrorObligaciones('');
    setLoadingObligaciones(false);
    if (!selectedVenta && form.clienteExternoId && form.obraExternoId) {
      setLoadingObligaciones(true);
      ventasService.obligacionesParaFactura({cliente:form.clienteExternoId, obra:form.obraExternoId, moneda:form.monedaCodigo})
        .then(rows => { if (actual) { setObligaciones(rows); setMonedaPlan(rows.some(o => o.monedaCodigo === form.monedaCodigo) ? form.monedaCodigo : rows[0]?.monedaCodigo || form.monedaCodigo); } })
        .catch(error => { if (actual) setErrorObligaciones(error.response?.data?.error || 'No se pudieron consultar las cuotas del plan de Vía 1.'); })
        .finally(() => { if (actual) setLoadingObligaciones(false); });
    }
    return () => { actual = false; };
  }, [selectedVenta, form.clienteExternoId, form.obraExternoId, form.monedaCodigo]);
  const ivaLinea = l => Math.round(Number(l.neto) * Number(tasas.find(t => t.id === Number(l.alicuotaId))?.porcentaje || 0)) / 100;
  const regimen = regimenes.find(r => String(r.id) === regimenId);
  const neto = lineas.reduce((s, l) => s + Number(l.neto), 0);
  const requiereConversion = monedaPlan === 'USD' && form.monedaCodigo === 'ARS';
  const cotizacionPlan = requiereConversion ? Number(referencia) : 1;
  const netoPlan = cotizacionPlan > 0 ? Math.round(neto / cotizacionPlan * 100) / 100 : 0;
  const aplicadoPlan = Object.values(aplicado).reduce((s, v) => s + Number(v || 0), 0);
  const cuotasPlan = obligaciones.filter(o => o.monedaCodigo === monedaPlan);
  const netoTotal = neto + Object.values(otros).reduce((s, v) => s + Number(v), 0);
  const subtotal = netoTotal + lineas.reduce((s, l) => s + ivaLinea(l), 0);
  const baseIibb = regimen?.tipoBaseCalculo === 'NetoGravado' ? neto : netoTotal;
  const iibb = regimen && baseIibb >= Number(regimen.montoMinimo || 0)
    ? Math.round(Math.round(baseIibb * 100) * Math.round(regimen.porcentaje * 10000) / 1000000) / 100 : 0;
  const moneda = n => form.monedaCodigo + ' ' + n.toLocaleString('es-AR', {minimumFractionDigits: 2, maximumFractionDigits: 2});
  const actualizarLinea = (i, campo, valor) => setLineas(prev => prev.map((l,j) => j===i ? {...l,[campo]:valor} : l));

  const isEditing = Boolean(selectedVenta?.id);
  const activeTipos = useMemo(() => tiposComprobante.filter((tipo) => tipo.activo), [tiposComprobante]);
  const selectedPunto = useMemo(
    () => puntosVenta.find((punto) => String(punto.puntoVentaComprobanteId) === form.puntoVentaComprobanteId),
    [puntosVenta, form.puntoVentaComprobanteId],
  );

  useEffect(() => {
    const tipoId = form.tipoComprobanteVentaId;
    const requestId = requestRef.current + 1;
    requestRef.current = requestId;

    Promise.resolve().then(() => {
      if (requestRef.current !== requestId) return;
      setPuntosError('');

      if (!tipoId) {
        setPuntosVenta([]);
        setLoadingPuntos(false);
        setForm((prev) => ({ ...prev, puntoVentaComprobanteId: '', puntoVenta: '' }));
        return;
      }

      setLoadingPuntos(true);
      ventasService.getPuntosVentaPorComprobante(
        tipoId,
        selectedVenta?.puntoVentaComprobanteId ? { relacionActualId: selectedVenta.puntoVentaComprobanteId } : {},
      )
        .then((data) => {
          if (requestRef.current !== requestId) return;
          const nextPuntos = data || [];
          setPuntosVenta(nextPuntos);
          setForm((prev) => {
            const current = nextPuntos.find((punto) => String(punto.puntoVentaComprobanteId) === prev.puntoVentaComprobanteId);
            const matchingLegacyPoint = !prev.puntoVentaComprobanteId && prev.puntoVenta
              ? nextPuntos.find((punto) => punto.numero === Number(prev.puntoVenta))
              : null;
            const autoSelected = nextPuntos.length === 1 ? nextPuntos[0] : null;
            const selected = current || matchingLegacyPoint || autoSelected;

            return {
              ...prev,
              puntoVentaComprobanteId: selected ? String(selected.puntoVentaComprobanteId) : '',
              puntoVenta: selected ? String(selected.numero) : '',
            };
          });
        })
        .catch((error) => {
          if (requestRef.current !== requestId) return;
          setPuntosVenta([]);
          setPuntosError(error?.response?.data?.error || 'No se pudieron cargar los puntos de venta habilitados.');
          setForm((prev) => ({ ...prev, puntoVentaComprobanteId: '', puntoVenta: '' }));
        })
        .finally(() => {
          if (requestRef.current === requestId) setLoadingPuntos(false);
        });
    });

    return undefined;
  }, [form.tipoComprobanteVentaId, selectedVenta?.puntoVentaComprobanteId]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    if (name === 'fechaComprobante' || name === 'tipoComprobanteVentaId') setRegimenId('');
    setForm((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'monedaCodigo' && value.trim().toUpperCase() === 'ARS') {
        next.cotizacion = '1';
      }
      if (name === 'tipoComprobanteVentaId') {
        next.puntoVentaComprobanteId = '';
        next.puntoVenta = '';
      }
      if (name === 'puntoVentaComprobanteId') {
        const punto = puntosVenta.find((item) => String(item.puntoVentaComprobanteId) === value);
        next.puntoVenta = punto ? String(punto.numero) : '';
      }
      return next;
    });
  };

  const handleClienteObraChange = (values) => {
    setForm((prev) => ({
      ...prev,
      clienteExternoId: values.clienteExternoId ?? prev.clienteExternoId,
      obraExternoId: values.obraExternoId ?? values.obraExternaId ?? '',
    }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    if (!isEditing && obligaciones.length > 0 && neto > 0 &&
        (!cotizacionPlan || Math.round(aplicadoPlan * 100) !== Math.round(netoPlan * 100))) {
      setErrorFiscal('Distribuí el neto completo en la moneda del plan antes de registrar.');
      return;
    }
    if (!form.puntoVentaComprobanteId) {
      setPuntosError('Seleccione un punto de venta habilitado para el comprobante indicado.');
      return;
    }
    if (selectedPunto && !selectedPunto.habilitado) {
      setPuntosError('Seleccione una combinacion activa antes de guardar.');
      return;
    }

    onSubmit({
      tipoComprobanteVentaId: Number(form.tipoComprobanteVentaId),
      puntoVentaComprobanteId: form.puntoVentaComprobanteId ? Number(form.puntoVentaComprobanteId) : null,
      clienteExternoId: String(form.clienteExternoId),
      obraExternaId: String(form.obraExternoId),
      fechaComprobante: form.fechaComprobante,
      puntoVenta: Number(form.puntoVenta),
      numeroComprobante: Number(form.numeroComprobante),
      monedaCodigo: form.monedaCodigo.trim().toUpperCase(),
      cotizacion: Number(form.cotizacion),
      ...(!isEditing && form.monedaCodigo === 'ARS' && referencia !== '' && {cotizacionDolarReferencia: Number(referencia)}),
      observaciones: form.observaciones,
      ...(!isEditing && {regimenPercepcionIibbId: regimenId ? Number(regimenId) : null, lineas:lineas.filter(l => l.neto || l.alicuotaId).map(l => ({neto:Number(l.neto), alicuotaId:Number(l.alicuotaId)})),
        ...Object.fromEntries(Object.entries(otros).map(([k,v]) => [k,Number(v)])),
        aplicaciones:Object.entries(aplicado).filter(([,v]) => Number(v)>0).map(([id,v]) => ({obligacionId:Number(id),importeAplicado:Number(v)}))}),
    });
  };

  return (
    <form className="venta-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <div className="form-field">
          <label>Tipo de comprobante</label>
          <select
            name="tipoComprobanteVentaId"
            value={form.tipoComprobanteVentaId}
            onChange={handleChange}
            required
          >
            <option value="">Seleccionar</option>
            {activeTipos.map((tipo) => (
              <option key={tipo.id} value={tipo.id}>
                {tipo.descripcion}
              </option>
            ))}
          </select>
        </div>

        <div className="form-field">
          <label>Punto de venta</label>
          <select
            name="puntoVentaComprobanteId"
            value={form.puntoVentaComprobanteId}
            onChange={handleChange}
            disabled={!form.tipoComprobanteVentaId || loadingPuntos || !puntosVenta.length}
            required
          >
            <option value="">
              {!form.tipoComprobanteVentaId ? 'Seleccione primero un comprobante' : loadingPuntos ? 'Cargando...' : 'Seleccionar'}
            </option>
            {puntosVenta.map((punto) => (
              <option key={punto.puntoVentaComprobanteId} value={punto.puntoVentaComprobanteId}>
                {punto.habilitado ? punto.textoMostrar : `${punto.textoMostrar} (no habilitado)`}
              </option>
            ))}
          </select>
          <div className="field-help">
            {form.tipoComprobanteVentaId
              ? 'Se muestran unicamente los puntos habilitados para el comprobante seleccionado.'
              : 'Seleccione primero un tipo de comprobante.'}
          </div>
          {form.tipoComprobanteVentaId && !loadingPuntos && !puntosVenta.length && !puntosError && (
            <p className="form-warning">No existen puntos de venta activos habilitados para el comprobante seleccionado. Revise la Parametrizacion de Ventas.</p>
          )}
          {selectedPunto && !selectedPunto.habilitado && (
            <p className="form-warning">El punto de venta actualmente asignado ya no esta habilitado para este comprobante. Seleccione una combinacion activa antes de guardar.</p>
          )}
          {puntosError && <p className="form-error">{puntosError}</p>}
        </div>

      </div>

      <ClienteObraSelector
        clienteExternoId={form.clienteExternoId}
        obraExternoId={form.obraExternoId}
        onChange={handleClienteObraChange}
      />

      <div className="form-grid">
        <div className="form-field">
          <label>Fecha</label>
          <input name="fechaComprobante" type="date" value={form.fechaComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Numero</label>
          <input name="numeroComprobante" inputMode="numeric" pattern="0*[1-9][0-9]*" maxLength={8} onBlur={() => setForm(prev => ({ ...prev, numeroComprobante: prev.numeroComprobante && prev.numeroComprobante.padStart(8, '0') }))} value={form.numeroComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Moneda</label>
          <select name="monedaCodigo" value={form.monedaCodigo} onChange={handleChange} required>
            <option value="ARS">ARS</option>
            {isEditing && <option value="USD">USD</option>}
          </select>
        </div>

        <div className="form-field">
          {!isEditing && form.monedaCodigo === 'ARS' ? <>
            <label htmlFor="usd-referencia">Cotización de la factura · ARS por USD</label>
            <NumericInput id="usd-referencia"  step="0.000001" min="0.000001" placeholder="Pesos por 1 USD" value={referencia} required={requiereConversion && cuotasPlan.length > 0 && neto > 0} onChange={e=>setReferencia(e.target.value)} />
            <small className="field-help">Se utiliza para imputar el neto a cuotas en dólares.</small>
          </> : <><label>Cotización</label>
          <NumericInput
            name="cotizacion"
            
            min="0.000001"
            step="0.000001"
            value={form.cotizacion}
            onChange={handleChange}
            disabled={form.monedaCodigo === 'ARS'}
            required
          /></>}
        </div>
      </div>

      <div className="form-field">
        <label>Observaciones</label>
        <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
      </div>

      {!isEditing && <fieldset className="factura-importes"><legend>Importes de la factura · {form.monedaCodigo}</legend>
        {errorFiscal && <p role="alert">{errorFiscal}</p>}
        {lineas.map((l,i) => <div className="form-grid" key={i}>
          <label>Neto gravado <NumericInput  min="0.01" step="0.01" value={l.neto} required={!!l.alicuotaId} onChange={e=>actualizarLinea(i,'neto',e.target.value)}/></label>
          <label>Alícuota IVA <select required={!!l.neto} value={l.alicuotaId} onChange={e=>actualizarLinea(i,'alicuotaId',e.target.value)}><option value="">Seleccionar</option>{tasas.map(t=><option key={t.id} value={t.id}>{t.descripcion} ({t.porcentaje}%)</option>)}</select></label>
          <span className="factura-iva">IVA débito <strong>{ivaLinea(l).toLocaleString('es-AR',{minimumFractionDigits:2})}</strong></span>
        </div>)}
        <button className="btn-secondary" type="button" onClick={()=>setLineas(lineas.length===1?[...lineas,{neto:'',alicuotaId:''}]:lineas.slice(0,1))}>{lineas.length===1?'Agregar otra alícuota':'Quitar segunda alícuota'}</button>
        <div className="form-grid">{Object.entries({noGravado:'Otros no gravados',exento:'Exento',devolucionCheques:'Devolución de cheques'}).map(([k,label])=><label key={k}>{label}<NumericInput  min="0" step="0.01" value={otros[k]} onChange={e=>setOtros({...otros,[k]:e.target.value})}/></label>)}</div>
        <AlicuotaIibbSelect regimenes={regimenes} fecha={form.fechaComprobante} value={regimenId} onChange={setRegimenId}
          disabled={tiposComprobante.find(t => String(t.id) === form.tipoComprobanteVentaId)?.esExportacion} />
        <div className="factura-subtotal">
          <span>Subtotal antes de IIBB <strong>{moneda(subtotal)}</strong></span>
          {regimen && <span>Base de percepción <strong>{moneda(baseIibb)}</strong></span>}
          <span>Percepción IIBB <strong>{moneda(iibb)}</strong></span>
          <span>Total factura <strong>{moneda(subtotal + iibb)}</strong></span>
        </div>
        <fieldset className="factura-importes">
          <legend>Cuotas del plan de pago · Vía 1</legend>
          {!form.clienteExternoId || !form.obraExternoId ? (
            <p className="field-help">Seleccioná cliente y obra para consultar las cuotas que podés facturar.</p>
          ) : loadingObligaciones ? (
            <p role="status">Consultando cuotas disponibles...</p>
          ) : errorObligaciones ? (
            <p className="form-error" role="alert">{errorObligaciones}</p>
          ) : obligaciones.length === 0 ? (
            <p className="field-help">No hay cuotas con saldo disponible para facturar en Vía 1 para este cliente y obra.</p>
          ) : (
            <>
              <label className="form-field">Moneda del plan
                <select value={monedaPlan} onChange={e => { setMonedaPlan(e.target.value); setAplicado({}); }}>
                  {[...new Set(obligaciones.map(o => o.monedaCodigo))].map(m => <option key={m} value={m}>{m}</option>)}
                </select>
              </label>
              <p className="field-help">Ingresá en {monedaPlan} la parte del neto que corresponde a cada cuota. Seleccioná cuotas de una misma moneda por factura.</p>
              {requiereConversion && !cotizacionPlan && <p className="form-warning">Completá la cotización de la factura para imputar a dólares.</p>}
              <div className="summary-grid">
                <div><span>Neto imputable en pesos</span><strong>{moneda(neto)}</strong></div>
                <div><span>Neto a distribuir</span><strong>{monedaPlan} {netoPlan.toFixed(2)}</strong></div>
                <div><span>Aplicado</span><strong>{monedaPlan} {aplicadoPlan.toFixed(2)}</strong></div>
                <div><span>Pendiente</span><strong>{monedaPlan} {(netoPlan - aplicadoPlan).toFixed(2)}</strong></div>
              </div>
              <div className="responsive-table-wrapper">
                <table className="data-table">
                  <thead><tr><th>Acuerdo</th><th>Cuota</th><th>Vencimiento</th><th>Disponible para facturar</th><th>Aplicar a esta factura</th></tr></thead>
                  <tbody>{cuotasPlan.map(o => (
                    <tr key={o.obligacionId}>
                      <td>{o.numeroAcuerdo}</td>
                      <td>{o.tipoObligacion} {o.numeroCuota}</td>
                      <td>{o.fechaVencimiento?.slice(0, 10)}</td>
                      <td>{o.monedaCodigo} {Number(o.saldoDisponible).toLocaleString('es-AR', { minimumFractionDigits: 2 })}</td>
                      <td><NumericInput aria-label={'Importe para ' + o.numeroAcuerdo + ' cuota ' + o.numeroCuota}
                        disabled={requiereConversion && !cotizacionPlan} min="0" max={o.saldoDisponible} step="0.01" value={aplicado[o.obligacionId] || ''}
                        onChange={e => setAplicado(prev => ({...prev, [o.obligacionId]: e.target.value}))} /></td>
                    </tr>
                  ))}</tbody>
                </table>
              </div>
            </>
          )}
        </fieldset>
      </fieldset>}
      {isEditing && <div className="venta-state-line">
        <span>Estado</span>
        <strong>{selectedVenta?.estado}</strong>
      </div>}

      <div className="form-actions">
        {isEditing && (
          <button className="btn-secondary" type="button" onClick={onCancel} disabled={saving}>
            Cancelar
          </button>
        )}
        <button className="btn-primary" type="submit" disabled={saving || loadingPuntos || loadingObligaciones || Boolean(errorObligaciones) || !form.puntoVentaComprobanteId || Boolean(selectedPunto && !selectedPunto.habilitado)}>
          {saving ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Registrar factura'}
        </button>
      </div>
    </form>
  );
};

export default VentaHeaderForm;

import React, { useEffect, useMemo, useState } from 'react';
import { cents, formatMoney } from '../revision/revisionDraft.js';
import AplicacionPagoForm from './AplicacionPagoForm';
import cobranzasService from '../../cobranzas/services/cobranzasService';

const imputaciones = [
  { value: 'Anticipo', label: 'Anticipo' },
  { value: 'SaldoGeneral', label: 'Pago' },
  { value: 'PagoParcial', label: 'Pago parcial' },
  { value: 'Hito', label: 'Hito' },
  { value: 'Cuota', label: 'Cuota' },
];

const PagoComercialForm = ({ acuerdo, onSubmit, loading, error, initialViaId = null, lockVia = false }) => {
  const vias = (acuerdo?.vias || []).filter((via) => via.viaOperacion === 'Via2');
  const initialVia = vias.find((via) => String(via.id) === String(initialViaId)) || vias[0];
  const firstViaId = initialVia?.id ? String(initialVia.id) : '';
  const [form, setForm] = useState({
    clienteExternoId: acuerdo?.clienteExternoId || '',
    obraExternaId: acuerdo?.obraExternaId || '',
    acuerdoComercialId: acuerdo?.id || '',
    acuerdoComercialViaId: firstViaId,
    monedaCodigo: initialVia?.monedaCodigo || '',
    fechaPago: '',
    importeTotal: '',
    medioPago: '',
    tipoImputacion: 'SaldoGeneral',
    hitoComercialViaId: '',
    observaciones: '',
    aplicaciones: []
  });
  const [mediosPago, setMediosPago] = useState([]);
  const [mediosLoading, setMediosLoading] = useState(true);
  const [mediosError, setMediosError] = useState('');
  const [mediosAttempt, setMediosAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setMediosLoading(true);
    setMediosError('');
    cobranzasService.getMediosPago(true)
      .then((medios) => { if (active) setMediosPago(medios); })
      .catch(() => { if (active) setMediosError('No se pudieron cargar los medios de pago.'); })
      .finally(() => { if (active) setMediosLoading(false); });
    return () => { active = false; };
  }, [mediosAttempt]);

  const selectedVia = vias.find((via) => String(via.id) === String(form.acuerdoComercialViaId));
  const planificada = selectedVia?.modalidadCobro === 'Planificada';
  const tipoImputacion = planificada ? 'Cuota' : form.tipoImputacion;
  const cuotas = useMemo(() => (selectedVia?.planPago?.cuotas || []).filter(c => !planificada ||
    (['Anticipo', 'Cuota'].includes(c.tipoCuota) && c.estado !== 'Anulada' && Number(c.saldoPendiente) > 0)), [selectedVia?.planPago?.cuotas, planificada]);
  const aplicado = form.aplicaciones.reduce((sum, item) => sum + (cents(item.importeAplicado) ?? 0n), 0n);
  const imputacionCompleta = !planificada || (cents(form.importeTotal) > 0n && aplicado === cents(form.importeTotal) && form.aplicaciones.every(item =>
    cents(item.importeAplicado) != null && cuotas.some(c => c.id === item.cuotaComercialId && cents(item.importeAplicado) <= cents(c.saldoPendiente))));
  const hitos = selectedVia?.hitos || [];

  useEffect(() => {
    const via2 = (acuerdo?.vias || []).filter((via) => via.viaOperacion === 'Via2');
    const nextVia = via2.find((via) => String(via.id) === String(initialViaId)) || via2[0];
    setForm((prev) => ({
      ...prev,
      clienteExternoId: acuerdo?.clienteExternoId || '',
      obraExternaId: acuerdo?.obraExternaId || '',
      acuerdoComercialId: acuerdo?.id || '',
      acuerdoComercialViaId: nextVia?.id ? String(nextVia.id) : '',
      monedaCodigo: nextVia?.monedaCodigo || '',
      aplicaciones: []
    }));
  }, [acuerdo, initialViaId]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    if (name === 'acuerdoComercialViaId') {
      const nextVia = vias.find((via) => String(via.id) === String(value));
      setForm({ ...form, acuerdoComercialViaId: value, monedaCodigo: nextVia?.monedaCodigo || '', aplicaciones: [], hitoComercialViaId: '' });
      return;
    }
    if (name === 'tipoImputacion') {
      setForm({ ...form, tipoImputacion: value, aplicaciones: [], hitoComercialViaId: '' });
      return;
    }
    setForm({ ...form, [name]: value });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const importeTotal = Number(form.importeTotal);
    if (importeTotal <= 0 || !selectedVia || !imputacionCompleta || mediosLoading || mediosError || !mediosPago.some(medio => medio.descripcion === form.medioPago)) return;

    const aplicaciones = tipoImputacion === 'Cuota'
      ? form.aplicaciones.filter((item) => item.importeAplicado > 0).map((item) => ({ ...item, tipoImputacion: 'Cuota' }))
      : tipoImputacion === 'Hito'
        ? [{ hitoComercialViaId: Number(form.hitoComercialViaId), importeAplicado: importeTotal, tipoImputacion: 'Hito', observaciones: form.observaciones }]
        : [];

    onSubmit({
      ...form,
      tipoImputacion,
      acuerdoComercialViaId: Number(form.acuerdoComercialViaId),
      hitoComercialViaId: !planificada && form.hitoComercialViaId ? Number(form.hitoComercialViaId) : null,
      importeTotal,
      fechaPago: new Date(form.fechaPago).toISOString(),
      aplicaciones
    });
  };

  if (!vias.length) {
    return <p className="empty-state">Este acuerdo no tiene Via2. Los pagos de Via1 se registran desde el modulo Ventas.</p>;
  }

  return (
    <form className="form-grid" onSubmit={handleSubmit}>
      <div className="form-field">
        <label>Acuerdo</label>
        <input value={acuerdo?.numeroAcuerdo || ''} disabled />
      </div>
      <div className="form-field">
        <label>Via</label>
        <select name="acuerdoComercialViaId" value={form.acuerdoComercialViaId} onChange={handleChange} required disabled={lockVia}>
          {vias.map((via) => (
            <option key={via.id} value={via.id}>{via.viaOperacion} - {via.monedaCodigo}</option>
          ))}
        </select>
      </div>
      <div className="form-field">
        <label>Moneda</label>
        <input value={form.monedaCodigo || ''} disabled />
      </div>
      <div className="form-field">
        <label>Tipo de imputacion</label>
        <select name="tipoImputacion" value={tipoImputacion} onChange={handleChange} disabled={planificada}>
          {(planificada ? [{ value: 'Cuota', label: 'Anticipo y/o cuotas del plan' }] : imputaciones).map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
        </select>
      </div>
      <div className="form-field">
        <label>Fecha de pago</label>
        <input type="date" name="fechaPago" value={form.fechaPago} onChange={handleChange} required />
      </div>
      <div className="form-field">
        <label>Importe total</label>
        <input type="number" min="0.01" step="0.01" name="importeTotal" value={form.importeTotal} onChange={handleChange} required />
      </div>
      <div className="form-field">
        <label htmlFor="pago-comercial-medio">Medio de pago</label>
        <select id="pago-comercial-medio" name="medioPago" value={form.medioPago} onChange={handleChange} required disabled={loading || mediosLoading || !!mediosError}>
          <option value="">{mediosLoading ? 'Cargando medios de pago...' : 'Seleccioná un medio de pago'}</option>
          {mediosPago.map((medio) => <option key={medio.id} value={medio.descripcion}>{medio.descripcion}</option>)}
        </select>
        {mediosError && <div role="alert"><p className="form-error">{mediosError}</p><button type="button" className="btn-secondary" onClick={() => setMediosAttempt(attempt => attempt + 1)}>Reintentar carga de medios</button></div>}
        {!mediosLoading && !mediosError && !mediosPago.length && <p role="status">No hay medios de pago activos disponibles.</p>}
      </div>
      {tipoImputacion === 'Hito' && (
        <div className="form-field">
          <label>Hito</label>
          <select name="hitoComercialViaId" value={form.hitoComercialViaId} onChange={handleChange} required>
            <option value="">Selecciona un hito</option>
            {hitos.map((hito) => <option key={hito.id} value={hito.id}>{hito.descripcion}</option>)}
          </select>
        </div>
      )}
      <div className="form-field full-width">
        <label>Observaciones</label>
        <textarea name="observaciones" value={form.observaciones} onChange={handleChange} rows="3" />
      </div>

      {tipoImputacion === 'Cuota' && (
        <div className="full-width payment-applications">
          <h3>Aplicación al anticipo y/o cuotas</h3>
          {selectedVia?.planPago ? (
            <AplicacionPagoForm
              key={`${acuerdo?.id}-${selectedVia.id}`}
              cuotas={cuotas}
              monedaCodigo={selectedVia.monedaCodigo}
              onChange={(aplicaciones) => setForm({ ...form, aplicaciones })}
              pagoTotal={Number(form.importeTotal)}
            />
          ) : (
            <p className="empty-state">La via seleccionada no tiene plan de pago.</p>
          )}
          {planificada && <p role="status">Aplicado: {formatMoney(aplicado, form.monedaCodigo)}. Debe distribuir todo el importe del pago entre el anticipo y/o cuotas pendientes. Puede pagar parcialmente una obligación, pero no dejar parte del pago sin imputar.</p>}
        </div>
      )}

      {error && <p className="form-error full-width">{error}</p>}
      <div className="form-footer full-width">
        <button className="btn-primary" type="submit" disabled={loading || !imputacionCompleta || mediosLoading || !!mediosError || !mediosPago.some(medio => medio.descripcion === form.medioPago) || !selectedVia || (tipoImputacion === 'Cuota' && !selectedVia?.planPago)}>
          {loading ? 'Guardando pago...' : 'Registrar pago comercial'}
        </button>
      </div>
    </form>
  );
};

export default PagoComercialForm;

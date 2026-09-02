import React, { useEffect, useState } from 'react';
import AplicacionPagoForm from './AplicacionPagoForm';

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

  const selectedVia = vias.find((via) => String(via.id) === String(form.acuerdoComercialViaId));
  const cuotas = selectedVia?.planPago?.cuotas || [];
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
    if (importeTotal <= 0 || !selectedVia) return;

    const aplicaciones = form.tipoImputacion === 'Cuota'
      ? form.aplicaciones.filter((item) => item.importeAplicado > 0).map((item) => ({ ...item, tipoImputacion: 'Cuota' }))
      : form.tipoImputacion === 'Hito'
        ? [{ hitoComercialViaId: Number(form.hitoComercialViaId), importeAplicado: importeTotal, tipoImputacion: 'Hito', observaciones: form.observaciones }]
        : [];

    onSubmit({
      ...form,
      acuerdoComercialViaId: Number(form.acuerdoComercialViaId),
      hitoComercialViaId: form.hitoComercialViaId ? Number(form.hitoComercialViaId) : null,
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
        <select name="tipoImputacion" value={form.tipoImputacion} onChange={handleChange}>
          {imputaciones.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
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
        <label>Medio de pago</label>
        <input name="medioPago" value={form.medioPago} onChange={handleChange} required />
      </div>
      {form.tipoImputacion === 'Hito' && (
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

      {form.tipoImputacion === 'Cuota' && (
        <div className="full-width payment-applications">
          <h3>Aplicacion a cuotas</h3>
          {selectedVia?.planPago ? (
            <AplicacionPagoForm
              cuotas={cuotas}
              monedaCodigo={selectedVia.monedaCodigo}
              onChange={(aplicaciones) => setForm({ ...form, aplicaciones })}
              pagoTotal={Number(form.importeTotal)}
            />
          ) : (
            <p className="empty-state">La via seleccionada no tiene plan de pago.</p>
          )}
        </div>
      )}

      {error && <p className="form-error full-width">{error}</p>}
      <div className="form-footer full-width">
        <button className="btn-primary" type="submit" disabled={loading || !selectedVia || (form.tipoImputacion === 'Cuota' && !selectedVia?.planPago)}>
          {loading ? 'Guardando pago...' : 'Registrar pago comercial'}
        </button>
      </div>
    </form>
  );
};

export default PagoComercialForm;

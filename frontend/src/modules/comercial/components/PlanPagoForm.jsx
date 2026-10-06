import React, { useState } from 'react';
import { calendarDateToApi } from '../../../shared/utils/calendarDate';

const PlanPagoForm = ({ onSubmit, loading }) => {
  const [form, setForm] = useState({
    tieneAnticipo: false,
    montoAnticipo: '',
    fechaAnticipo: '',
    cantidadCuotas: '1',
    fechaPrimerVencimiento: '',
    periodicidad: 'Mensual',
    observaciones: ''
  });
  const [error, setError] = useState('');

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setForm({
      ...form,
      [name]: type === 'checkbox' ? checked : value,
      ...(name === 'tieneAnticipo' && !checked ? { fechaAnticipo: '', montoAnticipo: '' } : {})
    });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const montoAnticipo = Number(form.montoAnticipo);
    const cantidadCuotas = Number(form.cantidadCuotas);

    if (form.tieneAnticipo && montoAnticipo <= 0) {
      setError('El monto del anticipo debe ser mayor a cero cuando se activa el anticipo.');
      return;
    }

    if (cantidadCuotas <= 0) {
      setError('La cantidad de cuotas debe ser mayor a cero.');
      return;
    }

    if (!calendarDateToApi(form.fechaPrimerVencimiento) || (form.tieneAnticipo && !calendarDateToApi(form.fechaAnticipo))) {
      setError('Complete las fechas de vencimiento con fechas válidas.');
      return;
    }

    setError('');
    onSubmit({
      ...form,
      montoAnticipo: Number(form.montoAnticipo),
      cantidadCuotas,
      fechaAnticipo: form.tieneAnticipo ? calendarDateToApi(form.fechaAnticipo) : null,
      fechaPrimerVencimiento: calendarDateToApi(form.fechaPrimerVencimiento)
    });
  };

  return (
    <form className="form-grid" onSubmit={handleSubmit}>
      <div className="form-field checkbox-field">
        <label>
          <input type="checkbox" name="tieneAnticipo" checked={form.tieneAnticipo} onChange={handleChange} />
          Tiene anticipo
        </label>
      </div>
      <div className="form-field">
        <label>Monto de Anticipo</label>
        <input type="number" min="0" step="0.01" name="montoAnticipo" value={form.montoAnticipo} onChange={handleChange} disabled={!form.tieneAnticipo} />
      </div>
      {form.tieneAnticipo && (
          <div className="form-field">
            <label htmlFor="fechaAnticipo">Fecha de vencimiento del anticipo</label>
            <input id="fechaAnticipo" type="date" name="fechaAnticipo" value={form.fechaAnticipo} onChange={handleChange} required />
          </div>
      )}
      <div className="form-field">
        <label>Cantidad de Cuotas</label>
        <input type="number" min="1" name="cantidadCuotas" value={form.cantidadCuotas} onChange={handleChange} required />
      </div>
      <div className="form-field">
        <label>Fecha del primer vencimiento</label>
        <input type="date" name="fechaPrimerVencimiento" value={form.fechaPrimerVencimiento} onChange={handleChange} required />
      </div>
      <div className="form-field">
        <label>Periodicidad</label>
        <select name="periodicidad" value={form.periodicidad} onChange={handleChange}>
          <option value="Mensual">Mensual</option>
          <option value="Quincenal">Quincenal</option>
          <option value="Semanal">Semanal</option>
          <option value="Anual">Anual</option>
        </select>
      </div>
      <div className="form-field full-width">
        <label>Observaciones</label>
        <textarea name="observaciones" value={form.observaciones} onChange={handleChange} rows="3" />
      </div>
      <div className="form-footer full-width">
        {error && <p className="form-error">{error}</p>}
        <button className="btn-primary" type="submit" disabled={loading}>
          {loading ? 'Generando cuotas...' : 'Generar plan de pago'}
        </button>
      </div>
    </form>
  );
};

export default PlanPagoForm;

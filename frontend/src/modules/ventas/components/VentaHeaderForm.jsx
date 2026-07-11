import { useMemo, useState } from 'react';
import ClienteObraSelector from '../../comercial/components/ClienteObraSelector';

const initialForm = {
  tipoComprobanteVentaId: '',
  clienteExternoId: '',
  obraExternoId: '',
  fechaComprobante: new Date().toISOString().slice(0, 10),
  puntoVenta: '',
  numeroComprobante: '',
  monedaCodigo: 'ARS',
  cotizacion: '1',
  observaciones: '',
};

const toDateInput = (value) => {
  if (!value) return new Date().toISOString().slice(0, 10);
  return new Date(value).toISOString().slice(0, 10);
};

const buildInitialForm = (selectedVenta) => {
  if (!selectedVenta) return initialForm;

  return {
    tipoComprobanteVentaId: String(selectedVenta.tipoComprobanteVentaId || ''),
    clienteExternoId: String(selectedVenta.clienteExternoId || ''),
    obraExternoId: String(selectedVenta.obraExternaId || ''),
    fechaComprobante: toDateInput(selectedVenta.fechaComprobante),
    puntoVenta: String(selectedVenta.puntoVenta || ''),
    numeroComprobante: String(selectedVenta.numeroComprobante || ''),
    monedaCodigo: selectedVenta.monedaCodigo || 'ARS',
    cotizacion: String(selectedVenta.cotizacion || 1),
    observaciones: selectedVenta.observaciones || '',
  };
};

const VentaHeaderForm = ({ tiposComprobante, selectedVenta, saving, onCancel, onSubmit }) => {
  const [form, setForm] = useState(() => buildInitialForm(selectedVenta));

  const isEditing = Boolean(selectedVenta?.id);
  const activeTipos = useMemo(() => tiposComprobante.filter((tipo) => tipo.activo), [tiposComprobante]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'monedaCodigo' && value.trim().toUpperCase() === 'ARS') {
        next.cotizacion = '1';
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
    onSubmit({
      tipoComprobanteVentaId: Number(form.tipoComprobanteVentaId),
      clienteExternoId: String(form.clienteExternoId),
      obraExternaId: String(form.obraExternoId),
      fechaComprobante: form.fechaComprobante,
      puntoVenta: Number(form.puntoVenta),
      numeroComprobante: Number(form.numeroComprobante),
      monedaCodigo: form.monedaCodigo.trim().toUpperCase(),
      cotizacion: Number(form.cotizacion),
      observaciones: form.observaciones,
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
          <label>Fecha</label>
          <input name="fechaComprobante" type="date" value={form.fechaComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Punto de venta</label>
          <input name="puntoVenta" type="number" min="1" value={form.puntoVenta} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Numero</label>
          <input name="numeroComprobante" type="number" min="1" value={form.numeroComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Moneda</label>
          <select name="monedaCodigo" value={form.monedaCodigo} onChange={handleChange} required>
            <option value="ARS">ARS</option>
            <option value="USD">USD</option>
          </select>
        </div>

        <div className="form-field">
          <label>Cotizacion</label>
          <input
            name="cotizacion"
            type="number"
            min="0.000001"
            step="0.000001"
            value={form.cotizacion}
            onChange={handleChange}
            disabled={form.monedaCodigo === 'ARS'}
            required
          />
        </div>
      </div>

      <ClienteObraSelector
        clienteExternoId={form.clienteExternoId}
        obraExternoId={form.obraExternoId}
        onChange={handleClienteObraChange}
      />

      <div className="form-field">
        <label>Observaciones</label>
        <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
      </div>

      <div className="venta-state-line">
        <span>Estado</span>
        <strong>{selectedVenta?.estado || 'Borrador'}</strong>
      </div>

      <div className="form-actions">
        {isEditing && (
          <button className="btn-secondary" type="button" onClick={onCancel} disabled={saving}>
            Cancelar
          </button>
        )}
        <button className="btn-primary" type="submit" disabled={saving}>
          {saving ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear borrador'}
        </button>
      </div>
    </form>
  );
};

export default VentaHeaderForm;

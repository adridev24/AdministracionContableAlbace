import { useState } from 'react';
import ClienteObraSelector from './ClienteObraSelector';

const todayInputValue = () => {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};
const dateInputToIso = (value) => `${value}T12:00:00.000Z`;

const initialForm = {
  clienteExternoId: '',
  obraExternoId: '',
  obraExternaId: '',
  numeroAcuerdo: '',
  fechaAcuerdo: todayInputValue(),
  descripcion: '',
  observaciones: '',
  vias: {
    Via1: { enabled: true, modalidadCobro: 'Planificada', monedaCodigo: 'USD', montoOriginal: '', observaciones: '' },
    Via2: { enabled: false, modalidadCobro: 'Abierta', monedaCodigo: 'USD', montoOriginal: '', observaciones: '' },
  },
};

const parseAmount = (value) => Number(String(value ?? '').replace(',', '.'));

const AcuerdoForm = ({ onSubmit, loading }) => {
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState('');

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
  };

  const handleViaChange = (via, field, value) => {
    setForm((prev) => ({
      ...prev,
      vias: {
        ...prev.vias,
        [via]: {
          ...prev.vias[via],
          [field]: value,
        },
      },
    }));
  };

  const buildViasPayload = () => Object.entries(form.vias)
    .filter(([, via]) => via.enabled)
    .map(([viaOperacion, via]) => ({
      viaOperacion,
      modalidadCobro: via.modalidadCobro,
      monedaCodigo: via.monedaCodigo,
      montoOriginal: parseAmount(via.montoOriginal),
      montoActual: parseAmount(via.montoOriginal),
      observaciones: via.observaciones,
    }));

  const handleSubmit = (e) => {
    e.preventDefault();
    const vias = buildViasPayload();
    const obraId = form.obraExternaId || form.obraExternoId;
    const hasInvalidVia = vias.some((via) => !Number.isFinite(via.montoOriginal) || via.montoOriginal <= 0 || !via.monedaCodigo);

    if (!form.clienteExternoId || !obraId || !form.numeroAcuerdo || !form.fechaAcuerdo) {
      setError('Completa cliente, obra, numero y fecha.');
      return;
    }

    if (!vias.length || hasInvalidVia) {
      setError('Completa al menos una via con moneda y monto mayor a cero.');
      return;
    }

    setError('');
    onSubmit({
      clienteExternoId: form.clienteExternoId,
      obraExternaId: obraId,
      numeroAcuerdo: form.numeroAcuerdo,
      fechaAcuerdo: dateInputToIso(form.fechaAcuerdo),
      descripcion: form.descripcion,
      observaciones: form.observaciones,
      estado: 'Borrador',
      vias,
    });
  };

  return (
    <form className="form-grid" onSubmit={handleSubmit}>
      <div className="form-field full-width">
        <ClienteObraSelector
          clienteExternoId={form.clienteExternoId}
          obraExternoId={form.obraExternaId || form.obraExternoId}
          onChange={(update) => setForm((prev) => ({ ...prev, ...update }))}
        />
      </div>
      <div className="form-field">
        <label>Numero de acuerdo</label>
        <input name="numeroAcuerdo" value={form.numeroAcuerdo} onChange={handleChange} placeholder="AC-2026-001" required />
      </div>
      <div className="form-field">
        <label>Fecha de acuerdo</label>
        <input type="date" name="fechaAcuerdo" value={form.fechaAcuerdo} onChange={handleChange} required />
      </div>
      <div className="form-field full-width">
        <label>Descripcion</label>
        <textarea name="descripcion" value={form.descripcion} onChange={handleChange} rows="3" />
      </div>

      <div className="commercial-vias-form full-width">
        {Object.entries(form.vias).map(([viaOperacion, via]) => (
          <section className="commercial-via-form" key={viaOperacion}>
            <label className="via-toggle">
              <input
                type="checkbox"
                checked={via.enabled}
                onChange={(event) => handleViaChange(viaOperacion, 'enabled', event.target.checked)}
              />
              {viaOperacion}
            </label>
            <div className="form-field">
              <label>Modalidad</label>
              <select
                value={via.modalidadCobro}
                onChange={(event) => handleViaChange(viaOperacion, 'modalidadCobro', event.target.value)}
                disabled={!via.enabled}
              >
                <option value="Planificada">Planificada</option>
                <option value="Abierta">Abierta</option>
              </select>
            </div>
            <div className="form-field">
              <label>Moneda</label>
              <select
                value={via.monedaCodigo}
                onChange={(event) => handleViaChange(viaOperacion, 'monedaCodigo', event.target.value)}
                disabled={!via.enabled}
              >
                <option value="ARS">ARS</option>
                <option value="USD">USD</option>
              </select>
            </div>
            <div className="form-field">
              <label>Monto</label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={via.montoOriginal}
                onChange={(event) => handleViaChange(viaOperacion, 'montoOriginal', event.target.value)}
                disabled={!via.enabled}
              />
            </div>
            <div className="form-field full-width">
              <label>Observaciones via</label>
              <textarea
                value={via.observaciones}
                onChange={(event) => handleViaChange(viaOperacion, 'observaciones', event.target.value)}
                disabled={!via.enabled}
                rows="4"
              />
            </div>
          </section>
        ))}
      </div>

      <div className="form-field full-width">
        <label>Observaciones generales</label>
        <textarea name="observaciones" value={form.observaciones} onChange={handleChange} rows="3" />
      </div>

      <div className="form-footer full-width">
        {error && <p className="form-error">{error}</p>}
        <button className="btn-primary" type="submit" disabled={loading}>
          {loading ? 'Guardando...' : 'Guardar acuerdo'}
        </button>
      </div>
    </form>
  );
};

export default AcuerdoForm;

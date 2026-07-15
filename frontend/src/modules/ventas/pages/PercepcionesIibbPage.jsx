import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  codigo: '',
  descripcion: '',
  jurisdiccion: 'Entre Rios',
  tipoTributo: 'PERCEPCION_IIBB',
  numeroRegimen: '',
  porcentaje: 0,
  tipoBaseCalculo: 'NetoGravado',
  montoMinimo: '',
  vigenciaDesde: '',
  vigenciaHasta: '',
  activo: true,
  orden: 0,
  observaciones: '',
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const formatPercent = (value) => `${Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}%`;
const formatDate = (value) => (value ? new Date(value).toLocaleDateString('es-AR') : '-');
const toDateInput = (value) => (value ? String(value).slice(0, 10) : '');

const PercepcionesIibbPage = () => {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [selected, setSelected] = useState(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('todos');
  const [vigenciaFilter, setVigenciaFilter] = useState('todas');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadItems = async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await ventasService.getPercepcionesIibb());
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    ventasService.getPercepcionesIibb()
      .then((data) => {
        if (mounted) setItems(data || []);
      })
      .catch((loadError) => {
        if (mounted) setError(getErrorMessage(loadError));
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, []);

  const filteredItems = useMemo(() => {
    const term = search.trim().toLowerCase();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return items.filter((item) => {
      const matchesTerm = !term ||
        item.codigo?.toLowerCase().includes(term) ||
        item.descripcion?.toLowerCase().includes(term) ||
        item.numeroRegimen?.toLowerCase().includes(term);
      const matchesStatus = statusFilter === 'todos' || (statusFilter === 'activos' ? item.activo : !item.activo);
      const desde = item.vigenciaDesde ? new Date(item.vigenciaDesde) : null;
      const hasta = item.vigenciaHasta ? new Date(item.vigenciaHasta) : null;
      const vigente = Boolean(desde && desde <= today && (!hasta || hasta >= today));
      const matchesVigencia = vigenciaFilter === 'todas' || (vigenciaFilter === 'vigentes' ? vigente : !vigente);
      return matchesTerm && matchesStatus && matchesVigencia;
    });
  }, [items, search, statusFilter, vigenciaFilter]);

  const handleChange = (event) => {
    const { checked, name, type, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleEdit = (item) => {
    setSelected(item);
    setForm({
      codigo: item.codigo || '',
      descripcion: item.descripcion || '',
      jurisdiccion: item.jurisdiccion || 'Entre Rios',
      tipoTributo: item.tipoTributo || 'PERCEPCION_IIBB',
      numeroRegimen: item.numeroRegimen || '',
      porcentaje: item.porcentaje ?? 0,
      tipoBaseCalculo: item.tipoBaseCalculo || 'NetoGravado',
      montoMinimo: item.montoMinimo ?? '',
      vigenciaDesde: toDateInput(item.vigenciaDesde),
      vigenciaHasta: toDateInput(item.vigenciaHasta),
      activo: Boolean(item.activo),
      orden: item.orden ?? 0,
      observaciones: item.observaciones || '',
    });
    setMessage('');
    setError('');
  };

  const resetForm = () => {
    setSelected(null);
    setForm(emptyForm);
  };

  const buildPayload = (override = {}) => ({
    codigo: String((override.codigo ?? form.codigo) || '').trim().toUpperCase(),
    descripcion: String((override.descripcion ?? form.descripcion) || '').trim(),
    jurisdiccion: String((override.jurisdiccion ?? form.jurisdiccion) || 'Entre Rios').trim(),
    tipoTributo: String((override.tipoTributo ?? form.tipoTributo) || 'PERCEPCION_IIBB').trim().toUpperCase(),
    numeroRegimen: String((override.numeroRegimen ?? form.numeroRegimen) || '').trim(),
    porcentaje: Number(override.porcentaje ?? form.porcentaje),
    tipoBaseCalculo: override.tipoBaseCalculo ?? form.tipoBaseCalculo,
    montoMinimo: (override.montoMinimo ?? form.montoMinimo) ? Number(override.montoMinimo ?? form.montoMinimo) : null,
    vigenciaDesde: override.vigenciaDesde ?? form.vigenciaDesde,
    vigenciaHasta: (override.vigenciaHasta ?? form.vigenciaHasta) || null,
    activo: override.activo ?? form.activo,
    orden: Number(override.orden ?? form.orden),
    observaciones: String((override.observaciones ?? form.observaciones) || '').trim() || null,
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = buildPayload();
      if (selected?.id) {
        await ventasService.updatePercepcionIibb(selected.id, payload);
        setMessage('Regimen de percepcion actualizado.');
      } else {
        await ventasService.createPercepcionIibb(payload);
        setMessage('Regimen de percepcion creado.');
      }
      resetForm();
      await loadItems();
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleToggle = async (item) => {
    if (item.activo && !window.confirm(`Desactivar ${item.codigo}?`)) return;
    setSaving(true);
    setMessage('');
    setError('');
    try {
      await ventasService.updatePercepcionIibb(item.id, buildPayload({ ...item, activo: !item.activo }));
      setMessage(item.activo ? 'Regimen desactivado.' : 'Regimen activado.');
      await loadItems();
    } catch (toggleError) {
      setError(getErrorMessage(toggleError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page-container ventas-page">
      <div className="page-header">
        <div>
          <h1>Percepciones IIBB Entre Rios</h1>
          <p className="page-subtitle">Regimenes configurables para futuras percepciones en facturas de venta.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar regimen' : 'Nuevo regimen'}>
        <form className="venta-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <div className="form-field">
              <label>Codigo</label>
              <input name="codigo" value={form.codigo} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Descripcion</label>
              <input name="descripcion" value={form.descripcion} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Jurisdiccion</label>
              <input name="jurisdiccion" value={form.jurisdiccion} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Tipo tributo</label>
              <input name="tipoTributo" value={form.tipoTributo} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Regimen</label>
              <input name="numeroRegimen" value={form.numeroRegimen} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Alicuota</label>
              <input name="porcentaje" type="number" min="0" step="0.0001" value={form.porcentaje} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Base de calculo</label>
              <select name="tipoBaseCalculo" value={form.tipoBaseCalculo} onChange={handleChange}>
                <option value="NetoGravado">Neto gravado</option>
                <option value="NetoTotal">Neto total</option>
                <option value="TotalSinIva">Total sin IVA</option>
                <option value="OtraBaseConfigurable">Otra base configurable</option>
              </select>
            </div>
            <div className="form-field">
              <label>Monto minimo</label>
              <input name="montoMinimo" type="number" min="0" step="0.01" value={form.montoMinimo} onChange={handleChange} />
            </div>
            <div className="form-field">
              <label>Vigencia desde</label>
              <input name="vigenciaDesde" type="date" value={form.vigenciaDesde} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Vigencia hasta</label>
              <input name="vigenciaHasta" type="date" value={form.vigenciaHasta} onChange={handleChange} />
            </div>
            <div className="form-field">
              <label>Orden</label>
              <input name="orden" type="number" min="0" value={form.orden} onChange={handleChange} />
            </div>
            <div className="form-field full-width">
              <label>Observaciones</label>
              <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
            </div>
          </div>
          <div className="field-help">No se aplican percepciones a facturas en esta etapa.</div>
          <div className="ventas-check-grid">
            <label><input name="activo" type="checkbox" checked={form.activo} onChange={handleChange} /> Activo</label>
          </div>
          <div className="form-actions">
            {selected && <button className="btn-secondary" type="button" onClick={resetForm} disabled={saving}>Cancelar</button>}
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button>
          </div>
        </form>
        {message && <p className="form-success">{message}</p>}
        {error && <p className="form-error">{error}</p>}
      </SectionCard>

      <SectionCard title="Regimenes existentes">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Codigo, descripcion o regimen" />
          </div>
          <div className="form-field">
            <label>Estado</label>
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="todos">Todos</option>
              <option value="activos">Activos</option>
              <option value="inactivos">Inactivos</option>
            </select>
          </div>
          <div className="form-field">
            <label>Vigencia</label>
            <select value={vigenciaFilter} onChange={(event) => setVigenciaFilter(event.target.value)}>
              <option value="todas">Todas</option>
              <option value="vigentes">Vigentes</option>
              <option value="no-vigentes">No vigentes</option>
            </select>
          </div>
        </div>
        {loading ? <LoadingSpinner /> : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Codigo</th>
                  <th>Descripcion</th>
                  <th>Regimen</th>
                  <th>Alicuota</th>
                  <th>Base</th>
                  <th>Vigencia</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.codigo}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.numeroRegimen}</td>
                    <td>{formatPercent(item.porcentaje)}</td>
                    <td>{item.tipoBaseCalculo}</td>
                    <td>{formatDate(item.vigenciaDesde)} / {formatDate(item.vigenciaHasta)}</td>
                    <td><span className={`status-pill ${item.activo ? 'is-active' : 'is-inactive'}`}>{item.activo ? 'Activo' : 'Inactivo'}</span></td>
                    <td className="row-actions">
                      <button className="btn-secondary" type="button" onClick={() => handleEdit(item)} disabled={saving}>Editar</button>
                      <button className="btn-secondary" type="button" onClick={() => handleToggle(item)} disabled={saving}>
                        {item.activo ? 'Desactivar' : 'Activar'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {!filteredItems.length && <p className="empty-state">No hay regimenes para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default PercepcionesIibbPage;

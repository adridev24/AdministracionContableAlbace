import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  codigo: '',
  descripcion: '',
  tipoTratamiento: 'Gravado',
  porcentaje: 21,
  activo: true,
  orden: 0,
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const formatPercent = (value) => `${Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}%`;

const AlicuotasIvaPage = () => {
  const [items, setItems] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [selected, setSelected] = useState(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('todos');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadItems = async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await ventasService.getAlicuotasIva());
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    ventasService.getAlicuotasIva()
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
    return items.filter((item) => {
      const matchesTerm = !term || item.codigo?.toLowerCase().includes(term) || item.descripcion?.toLowerCase().includes(term);
      const matchesStatus = statusFilter === 'todos' || (statusFilter === 'activos' ? item.activo : !item.activo);
      return matchesTerm && matchesStatus;
    });
  }, [items, search, statusFilter]);

  const handleChange = (event) => {
    const { checked, name, type, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleEdit = (item) => {
    setSelected(item);
    setForm({
      codigo: item.codigo || '',
      descripcion: item.descripcion || '',
      tipoTratamiento: item.tipoTratamiento || 'Gravado',
      porcentaje: item.porcentaje ?? 0,
      activo: Boolean(item.activo),
      orden: item.orden ?? 0,
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
    tipoTratamiento: override.tipoTratamiento ?? form.tipoTratamiento,
    porcentaje: Number(override.porcentaje ?? form.porcentaje),
    activo: override.activo ?? form.activo,
    orden: Number(override.orden ?? form.orden),
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = buildPayload();
      if (selected?.id) {
        await ventasService.updateAlicuotaIva(selected.id, payload);
        setMessage('Alicuota de IVA actualizada.');
      } else {
        await ventasService.createAlicuotaIva(payload);
        setMessage('Alicuota de IVA creada.');
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
      await ventasService.updateAlicuotaIva(item.id, buildPayload({ ...item, activo: !item.activo }));
      setMessage(item.activo ? 'Alicuota desactivada.' : 'Alicuota activada.');
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
          <h1>Tratamientos y alicuotas de IVA</h1>
          <p className="page-subtitle">Catalogo para futuros detalles de factura, sin calculos en esta etapa.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar alicuota' : 'Nueva alicuota'}>
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
              <label>Tipo de tratamiento</label>
              <select name="tipoTratamiento" value={form.tipoTratamiento} onChange={handleChange}>
                <option value="Gravado">Gravado</option>
                <option value="Exento">Exento</option>
                <option value="NoGravado">No gravado</option>
              </select>
            </div>
            <div className="form-field">
              <label>Porcentaje</label>
              <input name="porcentaje" type="number" min="0" step="0.0001" value={form.porcentaje} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Orden</label>
              <input name="orden" type="number" min="0" value={form.orden} onChange={handleChange} />
            </div>
          </div>
          <div className="field-help">El orden solo organiza visualmente listas y combos.</div>
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

      <SectionCard title="Alicuotas existentes">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Codigo o descripcion" />
          </div>
          <div className="form-field">
            <label>Estado</label>
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="todos">Todos</option>
              <option value="activos">Activos</option>
              <option value="inactivos">Inactivos</option>
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
                  <th>Tratamiento</th>
                  <th>Porcentaje</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.codigo}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.tipoTratamiento}</td>
                    <td>{formatPercent(item.porcentaje)}</td>
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
            {!filteredItems.length && <p className="empty-state">No hay alicuotas para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default AlicuotasIvaPage;

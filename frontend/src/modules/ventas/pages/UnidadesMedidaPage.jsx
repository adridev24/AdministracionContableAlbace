import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  codigo: '',
  descripcion: '',
  abreviatura: '',
  permiteDecimales: true,
  activo: true,
  orden: 0,
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';

const UnidadesMedidaPage = () => {
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
      setItems(await ventasService.getUnidadesMedida());
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    ventasService.getUnidadesMedida()
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
      const matchesTerm = !term ||
        item.codigo?.toLowerCase().includes(term) ||
        item.descripcion?.toLowerCase().includes(term) ||
        item.abreviatura?.toLowerCase().includes(term);
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
      abreviatura: item.abreviatura || '',
      permiteDecimales: Boolean(item.permiteDecimales),
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
    abreviatura: String((override.abreviatura ?? form.abreviatura) || '').trim() || null,
    permiteDecimales: override.permiteDecimales ?? form.permiteDecimales,
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
        await ventasService.updateUnidadMedida(selected.id, payload);
        setMessage('Unidad de medida actualizada.');
      } else {
        await ventasService.createUnidadMedida(payload);
        setMessage('Unidad de medida creada.');
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
      await ventasService.updateUnidadMedida(item.id, buildPayload({ ...item, activo: !item.activo }));
      setMessage(item.activo ? 'Unidad de medida desactivada.' : 'Unidad de medida activada.');
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
          <h1>Unidades de medida de venta</h1>
          <p className="page-subtitle">Unidades disponibles para parametrizar items facturables.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar unidad' : 'Nueva unidad'}>
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
              <label>Abreviatura</label>
              <input name="abreviatura" value={form.abreviatura} onChange={handleChange} />
            </div>
            <div className="form-field">
              <label>Orden</label>
              <input name="orden" type="number" min="0" value={form.orden} onChange={handleChange} />
            </div>
          </div>
          <div className="ventas-check-grid">
            <label><input name="permiteDecimales" type="checkbox" checked={form.permiteDecimales} onChange={handleChange} /> Permite decimales</label>
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

      <SectionCard title="Unidades existentes">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Codigo, descripcion o abreviatura" />
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
                  <th>Abrev.</th>
                  <th>Decimales</th>
                  <th>Orden</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.codigo}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.abreviatura || '-'}</td>
                    <td>{item.permiteDecimales ? 'Si' : 'No'}</td>
                    <td>{item.orden}</td>
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
            {!filteredItems.length && <p className="empty-state">No hay unidades para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default UnidadesMedidaPage;

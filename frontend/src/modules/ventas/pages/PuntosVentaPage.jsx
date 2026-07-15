import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  numero: '',
  descripcion: '',
  observaciones: '',
  activo: true,
  comprobantesPermitidosIds: [],
};

const formatPunto = (value) => String(value || 0).padStart(4, '0');
const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';

const PuntosVentaPage = () => {
  const [items, setItems] = useState([]);
  const [tiposComprobante, setTiposComprobante] = useState([]);
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
      const [puntos, tipos] = await Promise.all([
        ventasService.getPuntosVenta(),
        ventasService.getConfiguracionesComprobante({ soloActivos: true }),
      ]);
      setItems(puntos || []);
      setTiposComprobante(tipos || []);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    Promise.all([
      ventasService.getPuntosVenta(),
      ventasService.getConfiguracionesComprobante({ soloActivos: true }),
    ])
      .then(([puntos, tipos]) => {
        if (!mounted) return;
        setItems(puntos || []);
        setTiposComprobante(tipos || []);
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
      const matchesTerm = !term || formatPunto(item.numero).includes(term) || item.descripcion?.toLowerCase().includes(term);
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
      numero: String(item.numero || ''),
      descripcion: item.descripcion || '',
      observaciones: item.observaciones || '',
      activo: Boolean(item.activo),
      comprobantesPermitidosIds: (item.comprobantesPermitidos || [])
        .filter((relacion) => relacion.activo)
        .map((relacion) => relacion.tipoComprobanteVentaId),
    });
    setMessage('');
    setError('');
  };

  const resetForm = () => {
    setSelected(null);
    setForm(emptyForm);
  };

  const handleComprobanteToggle = (tipoId) => {
    setForm((prev) => {
      const current = new Set(prev.comprobantesPermitidosIds || []);
      if (current.has(tipoId)) current.delete(tipoId);
      else current.add(tipoId);
      return { ...prev, comprobantesPermitidosIds: Array.from(current) };
    });
  };

  const buildPayload = (override = {}) => ({
    numero: Number(override.numero ?? form.numero),
    descripcion: String((override.descripcion ?? form.descripcion) || '').trim(),
    observaciones: String((override.observaciones ?? form.observaciones) || '').trim() || null,
    activo: override.activo ?? form.activo,
    comprobantesPermitidosIds: override.comprobantesPermitidosIds ?? form.comprobantesPermitidosIds,
  });

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = buildPayload();
      if (selected?.id) {
        await ventasService.updatePuntoVenta(selected.id, payload);
        setMessage('Punto de venta actualizado.');
      } else {
        await ventasService.createPuntoVenta(payload);
        setMessage('Punto de venta creado.');
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
    if (item.activo && !window.confirm(`Desactivar punto de venta ${formatPunto(item.numero)}?`)) return;
    setSaving(true);
    setMessage('');
    setError('');
    try {
      await ventasService.updatePuntoVenta(item.id, {
        numero: item.numero,
        descripcion: item.descripcion,
        observaciones: item.observaciones,
        activo: !item.activo,
        comprobantesPermitidosIds: (item.comprobantesPermitidos || [])
          .filter((relacion) => relacion.activo)
          .map((relacion) => relacion.tipoComprobanteVentaId),
      });
      setMessage(item.activo ? 'Punto de venta desactivado.' : 'Punto de venta activado.');
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
          <h1>Puntos de venta</h1>
          <p className="page-subtitle">Administracion de puntos de venta disponibles para comprobantes de Ventas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar punto de venta' : 'Nuevo punto de venta'}>
        <form className="venta-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <div className="form-field">
              <label>Numero</label>
              <input name="numero" type="number" min="1" value={form.numero} onChange={handleChange} required />
            </div>
            <div className="form-field">
              <label>Descripcion</label>
              <input name="descripcion" value={form.descripcion} onChange={handleChange} required />
            </div>
            <div className="form-field full-width">
              <label>Observaciones</label>
              <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
            </div>
          </div>
          <div className="ventas-check-grid">
            <label><input name="activo" type="checkbox" checked={form.activo} onChange={handleChange} /> Activo</label>
          </div>
          <div className="param-subsection">
            <h3>Comprobantes permitidos</h3>
            <p>Define que comprobantes puede emitir este punto de venta.</p>
            <div className="permission-list">
              {tiposComprobante.map((tipo) => {
                const checked = form.comprobantesPermitidosIds.includes(tipo.id);
                return (
                  <label className="permission-item" key={tipo.id}>
                    <input type="checkbox" checked={checked} onChange={() => handleComprobanteToggle(tipo.id)} />
                    <span>
                      <strong>{tipo.descripcion}</strong>
                      <small>
                        {tipo.letra ? `Letra ${tipo.letra}` : 'Sin letra'} - {tipo.tipoFiscal}
                        {tipo.esExportacion ? ' - Exportacion' : ''}
                        {tipo.esCreditoElectronica ? ' - FCE' : ''}
                        {tipo.requiereNomenclador ? ' - Requiere nomenclador' : ''}
                      </small>
                    </span>
                    <em>{checked ? 'Permitido' : 'No permitido'}</em>
                  </label>
                );
              })}
              {!tiposComprobante.length && <p className="empty-state">No hay comprobantes activos disponibles.</p>}
            </div>
          </div>
          <div className="form-actions">
            {selected && <button className="btn-secondary" type="button" onClick={resetForm} disabled={saving}>Cancelar</button>}
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button>
          </div>
        </form>
        {message && <p className="form-success">{message}</p>}
        {error && <p className="form-error">{error}</p>}
      </SectionCard>

      <SectionCard title="Puntos existentes">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Numero o descripcion" />
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
                  <th>Numero</th>
                  <th>Descripcion</th>
                  <th>Observaciones</th>
                  <th>Comprobantes permitidos</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{formatPunto(item.numero)}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.observaciones || '-'}</td>
                    <td>
                      <div className="tag-row">
                        {(item.comprobantesPermitidos || []).filter((relacion) => relacion.activo).map((relacion) => (
                          <span className="tag" key={relacion.id}>{relacion.tipoComprobanteCodigo}</span>
                        ))}
                        {!(item.comprobantesPermitidos || []).some((relacion) => relacion.activo) && <span>-</span>}
                      </div>
                    </td>
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
            {!filteredItems.length && <p className="empty-state">No hay puntos de venta para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default PuntosVentaPage;

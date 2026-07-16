import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  codigo: '',
  descripcion: '',
  descripcionAmpliada: '',
  categoriaItemFacturableId: '',
  unidadMedidaVentaId: '',
  tratamientoIvaPredeterminadoId: '',
  nomencladorPredeterminadoId: '',
  precioPredeterminado: '',
  activo: true,
  orden: 0,
  observaciones: '',
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const toSelectValue = (value) => (value === null || value === undefined ? '' : String(value));
const formatMoney = (value) => (
  value === null || value === undefined || value === ''
    ? '-'
    : Number(value).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
);

const ItemsFacturablesPage = () => {
  const [items, setItems] = useState([]);
  const [categorias, setCategorias] = useState([]);
  const [unidades, setUnidades] = useState([]);
  const [alicuotas, setAlicuotas] = useState([]);
  const [nomencladores, setNomencladores] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [selected, setSelected] = useState(null);
  const [filters, setFilters] = useState({
    search: '',
    status: 'todos',
    categoriaId: '',
    unidadMedidaId: '',
    tratamientoIvaId: '',
    nomencladorId: '',
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const loadItems = async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await ventasService.getItemsFacturables());
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    Promise.all([
      ventasService.getItemsFacturables(),
      ventasService.getCategoriasItemsFacturables({ soloActivos: true }),
      ventasService.getUnidadesMedida({ soloActivos: true }),
      ventasService.getAlicuotasIva({ soloActivos: true }),
      ventasService.getNomencladores({ soloActivos: true }),
    ])
      .then(([itemsData, categoriasData, unidadesData, alicuotasData, nomencladoresData]) => {
        if (!mounted) return;
        setItems(itemsData || []);
        setCategorias(categoriasData || []);
        setUnidades(unidadesData || []);
        setAlicuotas(alicuotasData || []);
        setNomencladores(nomencladoresData || []);
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
    const term = filters.search.trim().toLowerCase();
    return items.filter((item) => {
      const matchesTerm = !term ||
        item.codigo?.toLowerCase().includes(term) ||
        item.descripcion?.toLowerCase().includes(term) ||
        item.descripcionAmpliada?.toLowerCase().includes(term);
      const matchesStatus = filters.status === 'todos' || (filters.status === 'activos' ? item.activo : !item.activo);
      const matchesCategoria = !filters.categoriaId || item.categoriaItemFacturableId === Number(filters.categoriaId);
      const matchesUnidad = !filters.unidadMedidaId || item.unidadMedidaVentaId === Number(filters.unidadMedidaId);
      const matchesIva = !filters.tratamientoIvaId || item.tratamientoIvaPredeterminadoId === Number(filters.tratamientoIvaId);
      const matchesNomenclador = !filters.nomencladorId || item.nomencladorPredeterminadoId === Number(filters.nomencladorId);
      return matchesTerm && matchesStatus && matchesCategoria && matchesUnidad && matchesIva && matchesNomenclador;
    });
  }, [items, filters]);

  const handleFormChange = (event) => {
    const { checked, name, type, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const resetForm = () => {
    setSelected(null);
    setForm(emptyForm);
  };

  const handleEdit = (item) => {
    setSelected(item);
    setForm({
      codigo: item.codigo || '',
      descripcion: item.descripcion || '',
      descripcionAmpliada: item.descripcionAmpliada || '',
      categoriaItemFacturableId: toSelectValue(item.categoriaItemFacturableId),
      unidadMedidaVentaId: toSelectValue(item.unidadMedidaVentaId),
      tratamientoIvaPredeterminadoId: toSelectValue(item.tratamientoIvaPredeterminadoId),
      nomencladorPredeterminadoId: toSelectValue(item.nomencladorPredeterminadoId),
      precioPredeterminado: item.precioPredeterminado ?? '',
      activo: Boolean(item.activo),
      orden: item.orden ?? 0,
      observaciones: item.observaciones || '',
    });
    setMessage('');
    setError('');
  };

  const buildPayload = (override = {}) => {
    const pick = (name) => (Object.prototype.hasOwnProperty.call(override, name) ? override[name] : form[name]);
    const precio = pick('precioPredeterminado');
    return {
      codigo: String(pick('codigo') || '').trim().toUpperCase(),
      descripcion: String(pick('descripcion') || '').trim(),
      descripcionAmpliada: String(pick('descripcionAmpliada') || '').trim() || null,
      categoriaItemFacturableId: pick('categoriaItemFacturableId') ? Number(pick('categoriaItemFacturableId')) : null,
      unidadMedidaVentaId: Number(pick('unidadMedidaVentaId')),
      tratamientoIvaPredeterminadoId: Number(pick('tratamientoIvaPredeterminadoId')),
      nomencladorPredeterminadoId: pick('nomencladorPredeterminadoId') ? Number(pick('nomencladorPredeterminadoId')) : null,
      precioPredeterminado: precio === null || precio === undefined || precio === '' ? null : Number(precio),
      activo: pick('activo'),
      orden: Number(pick('orden')),
      observaciones: String(pick('observaciones') || '').trim() || null,
    };
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = buildPayload();
      if (selected?.id) {
        await ventasService.updateItemFacturable(selected.id, payload);
        setMessage('Item facturable actualizado.');
      } else {
        await ventasService.createItemFacturable(payload);
        setMessage('Item facturable creado.');
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
      await ventasService.updateItemFacturable(item.id, buildPayload({ ...item, activo: !item.activo }));
      setMessage(item.activo ? 'Item facturable desactivado.' : 'Item facturable activado.');
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
          <h1>Items facturables</h1>
          <p className="page-subtitle">Catalogo independiente para servicios, productos o conceptos facturables.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar item facturable' : 'Nuevo item facturable'}>
        <form className="venta-form" onSubmit={handleSubmit}>
          <div className="form-grid">
            <div className="form-field">
              <label>Codigo</label>
              <input name="codigo" value={form.codigo} onChange={handleFormChange} required />
            </div>
            <div className="form-field">
              <label>Descripcion</label>
              <input name="descripcion" value={form.descripcion} onChange={handleFormChange} required />
            </div>
            <div className="form-field">
              <label>Categoria</label>
              <select name="categoriaItemFacturableId" value={form.categoriaItemFacturableId} onChange={handleFormChange}>
                <option value="">Sin categoria</option>
                {categorias.map((categoria) => (
                  <option key={categoria.id} value={categoria.id}>{categoria.descripcion}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Unidad de medida</label>
              <select name="unidadMedidaVentaId" value={form.unidadMedidaVentaId} onChange={handleFormChange} required>
                <option value="">Seleccionar</option>
                {unidades.map((unidad) => (
                  <option key={unidad.id} value={unidad.id}>{unidad.descripcion}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>IVA predeterminado</label>
              <select name="tratamientoIvaPredeterminadoId" value={form.tratamientoIvaPredeterminadoId} onChange={handleFormChange} required>
                <option value="">Seleccionar</option>
                {alicuotas.map((alicuota) => (
                  <option key={alicuota.id} value={alicuota.id}>{alicuota.descripcion}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Nomenclador FCE</label>
              <select name="nomencladorPredeterminadoId" value={form.nomencladorPredeterminadoId} onChange={handleFormChange}>
                <option value="">Sin nomenclador</option>
                {nomencladores.map((nomenclador) => (
                  <option key={nomenclador.id} value={nomenclador.id}>{nomenclador.descripcion}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Precio predeterminado</label>
              <input name="precioPredeterminado" type="number" min="0" step="0.0001" value={form.precioPredeterminado} onChange={handleFormChange} />
            </div>
            <div className="form-field">
              <label>Orden</label>
              <input name="orden" type="number" min="0" value={form.orden} onChange={handleFormChange} />
            </div>
            <div className="form-field full-width">
              <label>Descripcion ampliada</label>
              <textarea name="descripcionAmpliada" rows="3" value={form.descripcionAmpliada} onChange={handleFormChange} />
            </div>
            <div className="form-field full-width">
              <label>Observaciones</label>
              <textarea name="observaciones" rows="2" value={form.observaciones} onChange={handleFormChange} />
            </div>
          </div>
          <div className="ventas-check-grid">
            <label><input name="activo" type="checkbox" checked={form.activo} onChange={handleFormChange} /> Activo</label>
          </div>
          <div className="form-actions">
            {selected && <button className="btn-secondary" type="button" onClick={resetForm} disabled={saving}>Cancelar</button>}
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button>
          </div>
        </form>
        {message && <p className="form-success">{message}</p>}
        {error && <p className="form-error">{error}</p>}
      </SectionCard>

      <SectionCard title="Items existentes">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input name="search" value={filters.search} onChange={handleFilterChange} placeholder="Codigo o descripcion" />
          </div>
          <div className="form-field">
            <label>Estado</label>
            <select name="status" value={filters.status} onChange={handleFilterChange}>
              <option value="todos">Todos</option>
              <option value="activos">Activos</option>
              <option value="inactivos">Inactivos</option>
            </select>
          </div>
          <div className="form-field">
            <label>Categoria</label>
            <select name="categoriaId" value={filters.categoriaId} onChange={handleFilterChange}>
              <option value="">Todas</option>
              {categorias.map((categoria) => (
                <option key={categoria.id} value={categoria.id}>{categoria.descripcion}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>Unidad</label>
            <select name="unidadMedidaId" value={filters.unidadMedidaId} onChange={handleFilterChange}>
              <option value="">Todas</option>
              {unidades.map((unidad) => (
                <option key={unidad.id} value={unidad.id}>{unidad.descripcion}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>IVA</label>
            <select name="tratamientoIvaId" value={filters.tratamientoIvaId} onChange={handleFilterChange}>
              <option value="">Todos</option>
              {alicuotas.map((alicuota) => (
                <option key={alicuota.id} value={alicuota.id}>{alicuota.descripcion}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>Nomenclador</label>
            <select name="nomencladorId" value={filters.nomencladorId} onChange={handleFilterChange}>
              <option value="">Todos</option>
              {nomencladores.map((nomenclador) => (
                <option key={nomenclador.id} value={nomenclador.id}>{nomenclador.descripcion}</option>
              ))}
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
                  <th>Categoria</th>
                  <th>Unidad</th>
                  <th>IVA</th>
                  <th>Nomenclador</th>
                  <th>Precio</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.codigo}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.categoriaDescripcion || '-'}</td>
                    <td>{item.unidadMedidaAbreviatura || item.unidadMedidaDescripcion}</td>
                    <td>{item.tratamientoIvaDescripcion}</td>
                    <td>{item.nomencladorDescripcion || '-'}</td>
                    <td>{formatMoney(item.precioPredeterminado)}</td>
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
            {!filteredItems.length && <p className="empty-state">No hay items para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default ItemsFacturablesPage;

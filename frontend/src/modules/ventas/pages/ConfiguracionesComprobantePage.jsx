import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';
import '../ventas.css';

const emptyForm = {
  codigo: '',
  descripcion: '',
  letra: '',
  tipoFiscal: 'Local',
  esCreditoElectronica: false,
  esExportacion: false,
  requiereNomenclador: false,
  permiteIva: true,
  signo: 1,
  activo: true,
  orden: 0,
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';

const ConfiguracionesComprobantePage = () => {
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
      setItems(await ventasService.getConfiguracionesComprobante());
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    ventasService.getConfiguracionesComprobante()
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
      letra: item.letra || '',
      tipoFiscal: item.tipoFiscal || 'Local',
      esCreditoElectronica: Boolean(item.esCreditoElectronica),
      esExportacion: Boolean(item.esExportacion),
      requiereNomenclador: Boolean(item.requiereNomenclador),
      permiteIva: Boolean(item.permiteIva),
      signo: item.signo ?? 1,
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
    ...form,
    ...override,
    codigo: String((override.codigo ?? form.codigo) || '').trim().toUpperCase(),
    descripcion: String((override.descripcion ?? form.descripcion) || '').trim(),
    letra: String((override.letra ?? form.letra) || '').trim().toUpperCase() || null,
    tipoFiscal: String((override.tipoFiscal ?? form.tipoFiscal) || 'Local').trim(),
    signo: Number(override.signo ?? form.signo),
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
        await ventasService.updateConfiguracionComprobante(selected.id, payload);
        setMessage('Configuracion actualizada.');
      } else {
        await ventasService.createConfiguracionComprobante(payload);
        setMessage('Configuracion creada.');
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
      await ventasService.updateConfiguracionComprobante(item.id, {
        codigo: item.codigo,
        descripcion: item.descripcion,
        letra: item.letra,
        tipoFiscal: item.tipoFiscal,
        esCreditoElectronica: item.esCreditoElectronica,
        esExportacion: item.esExportacion,
        requiereNomenclador: item.requiereNomenclador,
        permiteIva: item.permiteIva,
        signo: item.signo,
        activo: !item.activo,
        orden: item.orden,
      });
      setMessage(item.activo ? 'Configuracion desactivada.' : 'Configuracion activada.');
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
          <h1>Configuraciones de comprobantes</h1>
          <p className="page-subtitle">Facturas, notas y modalidades disponibles para ventas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard title={selected ? 'Editar configuracion' : 'Nueva configuracion'}>
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
              <label>Letra</label>
              <input name="letra" value={form.letra} onChange={handleChange} maxLength="5" />
            </div>
            <div className="form-field">
              <label>Tipo fiscal</label>
              <select name="tipoFiscal" value={form.tipoFiscal} onChange={handleChange}>
                <option value="Local">Local</option>
                <option value="Exportacion">Exportacion</option>
              </select>
            </div>
            <div className="form-field">
              <label>Signo</label>
              <select name="signo" value={form.signo} onChange={handleChange}>
                <option value={1}>Suma</option>
                <option value={-1}>Resta</option>
              </select>
            </div>
            <div className="form-field">
              <label>Orden</label>
              <input name="orden" type="number" value={form.orden} onChange={handleChange} />
            </div>
          </div>
          <div className="ventas-check-grid">
            <label><input name="esExportacion" type="checkbox" checked={form.esExportacion} onChange={handleChange} /> Exportacion</label>
            <label><input name="esCreditoElectronica" type="checkbox" checked={form.esCreditoElectronica} onChange={handleChange} /> Credito electronica</label>
            <label><input name="requiereNomenclador" type="checkbox" checked={form.requiereNomenclador} onChange={handleChange} /> Requiere nomenclador</label>
            <label><input name="permiteIva" type="checkbox" checked={form.permiteIva} onChange={handleChange} /> Permite IVA</label>
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

      <SectionCard title="Configuraciones existentes">
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
                  <th>Letra</th>
                  <th>Fiscal</th>
                  <th>Caracteristicas</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.map((item) => (
                  <tr key={item.id}>
                    <td><strong>{item.codigo}</strong></td>
                    <td>{item.descripcion}</td>
                    <td>{item.letra || '-'}</td>
                    <td>{item.tipoFiscal}</td>
                    <td>
                      <div className="tag-row">
                        {item.esExportacion && <span className="tag">Exportacion</span>}
                        {item.esCreditoElectronica && <span className="tag">FCE</span>}
                        {item.requiereNomenclador && <span className="tag">Nomenclador</span>}
                        {item.permiteIva && <span className="tag">IVA</span>}
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
            {!filteredItems.length && <p className="empty-state">No hay configuraciones para los filtros seleccionados.</p>}
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default ConfiguracionesComprobantePage;

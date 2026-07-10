import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import cuentasContablesService from '../services/cuentasContablesService';
import '../contabilidad.css';

const tiposCuenta = ['Activo', 'Pasivo', 'Patrimonio Neto', 'Ingreso', 'Egreso'];

const emptyForm = {
  codigo: '',
  nombre: '',
  tipoCuenta: '',
  activa: true,
};

const getApiError = (error) => {
  if (error?.response?.status === 401 || error?.response?.status === 403) {
    return 'No tenes autorizacion para realizar esta accion.';
  }

  if (error?.response?.data?.error) {
    return error.response.data.error;
  }

  if (!error?.response) {
    return 'No se pudo conectar con la API.';
  }

  return 'Ocurrio un error inesperado.';
};

const PlanCuentasPage = () => {
  const [cuentas, setCuentas] = useState([]);
  const [filters, setFilters] = useState({ codigo: '', nombre: '', tipoCuenta: '', activa: '' });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [formError, setFormError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editingCuenta, setEditingCuenta] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const formErrors = useMemo(() => {
    const errors = {};
    if (!form.codigo.trim()) errors.codigo = 'El codigo es obligatorio.';
    if (!form.nombre.trim()) errors.nombre = 'El nombre es obligatorio.';
    if (!form.tipoCuenta) errors.tipoCuenta = 'El tipo de cuenta es obligatorio.';
    return errors;
  }, [form]);

  const hasFormErrors = Object.keys(formErrors).length > 0;

  const buildQuery = () => ({
    codigo: filters.codigo.trim() || undefined,
    nombre: filters.nombre.trim() || undefined,
    tipoCuenta: filters.tipoCuenta || undefined,
    activa: filters.activa === '' ? undefined : filters.activa === 'true',
  });

  const fetchCuentas = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await cuentasContablesService.getCuentas(buildQuery());
      setCuentas(data);
    } catch (fetchError) {
      setError(getApiError(fetchError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchCuentas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
  };

  const handleFormChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const openCreateForm = () => {
    setEditingCuenta(null);
    setForm(emptyForm);
    setFormError('');
    setSuccess('');
    setShowForm(true);
  };

  const openEditForm = (cuenta) => {
    setEditingCuenta(cuenta);
    setForm({
      codigo: cuenta.codigo,
      nombre: cuenta.nombre,
      tipoCuenta: cuenta.tipoCuenta,
      activa: cuenta.activa,
    });
    setFormError('');
    setSuccess('');
    setShowForm(true);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingCuenta(null);
    setForm(emptyForm);
    setFormError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (hasFormErrors) return;

    setSaving(true);
    setFormError('');
    setSuccess('');

    const payload = {
      codigo: form.codigo,
      nombre: form.nombre,
      tipoCuenta: form.tipoCuenta,
    };

    try {
      if (editingCuenta) {
        await cuentasContablesService.updateCuenta(editingCuenta.id, { ...payload, activa: form.activa });
        setSuccess('Cuenta actualizada correctamente.');
      } else {
        await cuentasContablesService.createCuenta(payload);
        setSuccess('Cuenta creada correctamente.');
      }
      closeForm();
      await fetchCuentas();
    } catch (saveError) {
      setFormError(getApiError(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (cuenta) => {
    if (!cuenta.activa) return;

    setError('');
    setSuccess('');
    try {
      await cuentasContablesService.deactivateCuenta(cuenta.id);
      setSuccess('Cuenta dada de baja correctamente.');
      await fetchCuentas();
    } catch (deactivateError) {
      setError(getApiError(deactivateError));
    }
  };

  const handleReactivate = async (cuenta) => {
    setError('');
    setSuccess('');
    try {
      await cuentasContablesService.updateCuenta(cuenta.id, {
        codigo: cuenta.codigo,
        nombre: cuenta.nombre,
        tipoCuenta: cuenta.tipoCuenta,
        activa: true,
      });
      setSuccess('Cuenta activada correctamente.');
      await fetchCuentas();
    } catch (activateError) {
      setError(getApiError(activateError));
    }
  };

  const clearFilters = () => {
    setFilters({ codigo: '', nombre: '', tipoCuenta: '', activa: '' });
  };

  return (
    <div className="page-container contabilidad-page">
      <div className="page-header">
        <div>
          <h1>Plan de Cuentas</h1>
          <p className="page-subtitle">Administracion de cuentas contables activas e historicas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/">Principal</Link>
          <button className="btn-primary" type="button" onClick={openCreateForm}>Nueva cuenta</button>
        </div>
      </div>

      <SectionCard
        title="Buscar cuentas"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={clearFilters}>Limpiar</button>
            <button className="btn-primary" type="button" onClick={fetchCuentas} disabled={loading}>
              {loading ? 'Buscando...' : 'Buscar'}
            </button>
          </>
        )}
      >
        <div className="filter-grid">
          <label className="form-row">
            Codigo
            <input name="codigo" value={filters.codigo} onChange={handleFilterChange} />
          </label>
          <label className="form-row">
            Nombre
            <input name="nombre" value={filters.nombre} onChange={handleFilterChange} />
          </label>
          <label className="form-row">
            Tipo de cuenta
            <select name="tipoCuenta" value={filters.tipoCuenta} onChange={handleFilterChange}>
              <option value="">Todas</option>
              {tiposCuenta.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
            </select>
          </label>
          <label className="form-row">
            Estado
            <select name="activa" value={filters.activa} onChange={handleFilterChange}>
              <option value="">Todas</option>
              <option value="true">Activas</option>
              <option value="false">Inactivas</option>
            </select>
          </label>
        </div>
      </SectionCard>

      {success && <p className="form-success">{success}</p>}
      {error && <p className="form-error">{error}</p>}

      <SectionCard title="Resultados">
        {loading ? (
          <LoadingSpinner />
        ) : (
          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Codigo</th>
                  <th>Nombre</th>
                  <th>Tipo de cuenta</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {cuentas.map((cuenta) => (
                  <tr key={cuenta.id}>
                    <td><strong>{cuenta.codigo}</strong></td>
                    <td>{cuenta.nombre}</td>
                    <td>{cuenta.tipoCuenta}</td>
                    <td>
                      <span className={`status-pill ${cuenta.activa ? 'is-active' : 'is-inactive'}`}>
                        {cuenta.activa ? 'Activa' : 'Inactiva'}
                      </span>
                    </td>
                    <td>
                      <div className="row-actions">
                        <button className="btn-secondary btn-small" type="button" onClick={() => openEditForm(cuenta)}>
                          Editar
                        </button>
                        {cuenta.activa ? (
                          <button className="btn-secondary btn-small danger" type="button" onClick={() => handleDeactivate(cuenta)}>
                            Dar de baja
                          </button>
                        ) : (
                          <button className="btn-secondary btn-small" type="button" onClick={() => handleReactivate(cuenta)}>
                            Activar
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {!cuentas.length && (
                  <tr>
                    <td colSpan="5">
                      <div className="empty-state">No se encontraron cuentas contables.</div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      {showForm && (
        <div className="modal-backdrop">
          <form className="modal-card cuenta-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <h2>{editingCuenta ? 'Editar cuenta' : 'Nueva cuenta'}</h2>
              <button className="modal-close" type="button" onClick={closeForm} aria-label="Cerrar">x</button>
            </div>

            <div className="modal-body">
              <label className="form-row">
                Codigo
                <input name="codigo" value={form.codigo} onChange={handleFormChange} />
                {formErrors.codigo && <span className="field-error">{formErrors.codigo}</span>}
              </label>
              <label className="form-row">
                Nombre
                <input name="nombre" value={form.nombre} onChange={handleFormChange} />
                {formErrors.nombre && <span className="field-error">{formErrors.nombre}</span>}
              </label>
              <label className="form-row">
                Tipo de cuenta
                <select name="tipoCuenta" value={form.tipoCuenta} onChange={handleFormChange}>
                  <option value="">Seleccionar</option>
                  {tiposCuenta.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
                </select>
                {formErrors.tipoCuenta && <span className="field-error">{formErrors.tipoCuenta}</span>}
              </label>
              {editingCuenta && (
                <label className="checkbox-inline">
                  <input type="checkbox" name="activa" checked={form.activa} onChange={handleFormChange} />
                  Activa
                </label>
              )}
              {formError && <p className="form-error">{formError}</p>}
            </div>

            <div className="modal-footer">
              <button className="btn-secondary" type="button" onClick={closeForm}>Cancelar</button>
              <button className="btn-primary" type="submit" disabled={saving || hasFormErrors}>
                {saving ? 'Guardando...' : 'Guardar'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};

export default PlanCuentasPage;

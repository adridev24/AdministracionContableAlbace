import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import cuentasContablesService from '../services/cuentasContablesService';
import configuracionesContablesService from '../services/configuracionesContablesService';
import '../contabilidad.css';

const emptyDetalle = () => ({ tipoMovimiento: 'Debe', concepto: '', cuentaContableId: '', esObligatorio: true, orden: 1 });
const conceptoDescriptions = {
  CLIENTES: 'Deudores por ventas / Total del cliente',
  VENTA_NETA: 'Neto de la venta',
  IVA_DEBITO: 'IVA Debito Fiscal',
  PERCEPCION_IIBB: 'Percepcion de Ingresos Brutos',
  CAJA: 'Caja',
  BANCO: 'Banco',
  RETENCIONES: 'Retenciones',
};

const emptyForm = () => ({
  codigoOperacion: '',
  descripcion: '',
  activa: true,
  detalles: [
    { ...emptyDetalle(), orden: 1 },
    { ...emptyDetalle(), tipoMovimiento: 'Haber', orden: 2 },
  ],
});

const getApiError = (error) => {
  if (error?.response?.status === 401 || error?.response?.status === 403) return 'No tenes autorizacion para realizar esta accion.';
  if (error?.response?.data?.error) return error.response.data.error;
  if (!error?.response) return 'No se pudo conectar con la API.';
  return 'Ocurrio un error inesperado.';
};

const ConfiguracionContablePage = () => {
  const [configuraciones, setConfiguraciones] = useState([]);
  const [cuentas, setCuentas] = useState([]);
  const [tiposOperacion, setTiposOperacion] = useState([]);
  const [filters, setFilters] = useState({ codigoOperacion: '', activa: '' });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [formError, setFormError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [readOnly, setReadOnly] = useState(false);
  const [editingConfig, setEditingConfig] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const selectedTipoOperacion = useMemo(
    () => tiposOperacion.find((tipo) => tipo.codigo === form.codigoOperacion.trim().toUpperCase()),
    [tiposOperacion, form.codigoOperacion],
  );

  const conceptosDisponibles = useMemo(() => {
    const conceptosOperacion = selectedTipoOperacion?.conceptosSugeridos || [];
    const conceptosExistentes = form.detalles
      .map((detalle) => detalle.concepto?.trim().toUpperCase())
      .filter(Boolean);
    const fallbackConceptos = tiposOperacion.flatMap((tipo) => tipo.conceptosSugeridos || []);
    const source = conceptosOperacion.length ? conceptosOperacion : fallbackConceptos;
    return [...new Set([...source, ...conceptosExistentes])]
      .filter(Boolean)
      .sort();
  }, [form.detalles, selectedTipoOperacion, tiposOperacion]);

  const formatConcepto = (concepto) => {
    const codigo = concepto?.trim().toUpperCase() || '';
    const descripcion = conceptoDescriptions[codigo];
    return descripcion ? `${codigo} - ${descripcion}` : codigo;
  };

  const buildQuery = () => ({
    codigoOperacion: filters.codigoOperacion.trim() || undefined,
    activa: filters.activa === '' ? undefined : filters.activa === 'true',
  });

  const fetchConfiguraciones = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await configuracionesContablesService.getConfiguraciones(buildQuery());
      setConfiguraciones(data);
    } catch (fetchError) {
      setError(getApiError(fetchError));
    } finally {
      setLoading(false);
    }
  };

  const fetchSupportData = async () => {
    try {
      const [cuentasData, tiposData] = await Promise.all([
        cuentasContablesService.getCuentas({ activa: true }),
        configuracionesContablesService.getTiposOperacion(),
      ]);
      setCuentas(cuentasData);
      setTiposOperacion(tiposData);
    } catch {
      setCuentas([]);
      setTiposOperacion([]);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchConfiguraciones();
    fetchSupportData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const validationErrors = useMemo(() => {
    const errors = [];
    if (!form.codigoOperacion.trim()) errors.push('El codigo de operacion es obligatorio.');
    if (!form.descripcion.trim()) errors.push('La descripcion es obligatoria.');
    if (form.detalles.length < 2) errors.push('La configuracion debe tener al menos dos detalles.');
    if (!form.detalles.some((detalle) => detalle.tipoMovimiento === 'Debe')) errors.push('Debe existir al menos un movimiento Debe.');
    if (!form.detalles.some((detalle) => detalle.tipoMovimiento === 'Haber')) errors.push('Debe existir al menos un movimiento Haber.');

    const conceptos = form.detalles.map((detalle) => detalle.concepto.trim().toUpperCase()).filter(Boolean);
    if (new Set(conceptos).size !== conceptos.length) errors.push('No se pueden repetir conceptos.');

    form.detalles.forEach((detalle, index) => {
      if (!detalle.tipoMovimiento) errors.push(`El detalle ${index + 1} no tiene tipo de movimiento.`);
      if (!detalle.concepto.trim()) errors.push(`El detalle ${index + 1} no tiene concepto.`);
      if (!detalle.cuentaContableId) errors.push(`El detalle ${index + 1} no tiene cuenta contable.`);
    });

    return errors;
  }, [form]);

  const canSave = validationErrors.length === 0 && !saving && !readOnly;

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
  };

  const clearFilters = () => {
    setFilters({ codigoOperacion: '', activa: '' });
  };

  const handleHeaderChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleDetalleChange = (index, field, value) => {
    setForm((current) => ({
      ...current,
      detalles: current.detalles.map((detalle, detalleIndex) => (
        detalleIndex === index ? { ...detalle, [field]: value } : detalle
      )),
    }));
  };

  const addDetalle = () => {
    setForm((current) => ({
      ...current,
      detalles: [...current.detalles, { ...emptyDetalle(), orden: current.detalles.length + 1 }],
    }));
  };

  const removeDetalle = (index) => {
    setForm((current) => ({
      ...current,
      detalles: current.detalles.filter((_, detalleIndex) => detalleIndex !== index),
    }));
  };

  const openCreateForm = () => {
    setEditingConfig(null);
    setReadOnly(false);
    setForm(emptyForm());
    setFormError('');
    setSuccess('');
    setShowForm(true);
  };

  const openExisting = async (configuracion, viewOnly) => {
    setFormError('');
    setSuccess('');
    try {
      const detalle = await configuracionesContablesService.getConfiguracion(configuracion.id);
      setEditingConfig(detalle);
      setReadOnly(viewOnly);
      setForm({
        codigoOperacion: detalle.codigoOperacion,
        descripcion: detalle.descripcion,
        activa: detalle.activa,
        detalles: detalle.detalles.map((item) => ({
          tipoMovimiento: item.tipoMovimiento,
          concepto: item.concepto,
          cuentaContableId: String(item.cuentaContableId),
          esObligatorio: item.esObligatorio ?? true,
          orden: item.orden,
        })),
      });
      setShowForm(true);
    } catch (loadError) {
      setError(getApiError(loadError));
    }
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingConfig(null);
    setReadOnly(false);
    setForm(emptyForm());
    setFormError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!canSave) return;

    setSaving(true);
    setFormError('');
    setSuccess('');

    const payload = {
      codigoOperacion: form.codigoOperacion,
      descripcion: form.descripcion,
      activa: form.activa,
      detalles: form.detalles.map((detalle, index) => ({
        tipoMovimiento: detalle.tipoMovimiento,
        concepto: detalle.concepto,
        cuentaContableId: Number(detalle.cuentaContableId),
        esObligatorio: detalle.esObligatorio,
        orden: Number(detalle.orden) || index + 1,
      })),
    };

    try {
      if (editingConfig) {
        await configuracionesContablesService.updateConfiguracion(editingConfig.id, payload);
        setSuccess('Configuracion actualizada correctamente.');
      } else {
        await configuracionesContablesService.createConfiguracion(payload);
        setSuccess('Configuracion creada correctamente.');
      }
      closeForm();
      await fetchConfiguraciones();
    } catch (saveError) {
      setFormError(getApiError(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (configuracion) => {
    setError('');
    setSuccess('');
    try {
      await configuracionesContablesService.deactivateConfiguracion(configuracion.id);
      setSuccess('Configuracion dada de baja correctamente.');
      await fetchConfiguraciones();
    } catch (deactivateError) {
      setError(getApiError(deactivateError));
    }
  };

  const handleActivate = async (configuracion) => {
    setError('');
    setSuccess('');
    try {
      const detalle = await configuracionesContablesService.getConfiguracion(configuracion.id);
      await configuracionesContablesService.updateConfiguracion(configuracion.id, {
        codigoOperacion: detalle.codigoOperacion,
        descripcion: detalle.descripcion,
        activa: true,
        detalles: detalle.detalles.map((item) => ({
          tipoMovimiento: item.tipoMovimiento,
          concepto: item.concepto,
          cuentaContableId: item.cuentaContableId,
          esObligatorio: item.esObligatorio ?? true,
          orden: item.orden,
        })),
      });
      setSuccess('Configuracion activada correctamente.');
      await fetchConfiguraciones();
    } catch (activateError) {
      setError(getApiError(activateError));
    }
  };

  return (
    <div className="page-container contabilidad-page">
      <div className="page-header">
        <div>
          <h1>Configuracion Contable</h1>
          <p className="page-subtitle">Asociacion flexible entre operaciones del sistema y cuentas contables.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/contabilidad">Contabilidad</Link>
          <button className="btn-primary" type="button" onClick={openCreateForm}>Nueva configuracion</button>
        </div>
      </div>

      <SectionCard
        title="Buscar configuraciones"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={clearFilters}>Limpiar</button>
            <button className="btn-primary" type="button" onClick={fetchConfiguraciones} disabled={loading}>
              {loading ? 'Buscando...' : 'Buscar'}
            </button>
          </>
        )}
      >
        <div className="filter-grid">
          <label className="form-row">
            Codigo de operacion
            <input name="codigoOperacion" value={filters.codigoOperacion} onChange={handleFilterChange} />
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
                  <th>Codigo de operacion</th>
                  <th>Descripcion</th>
                  <th>Cuentas configuradas</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {configuraciones.map((configuracion) => (
                  <tr key={configuracion.id}>
                    <td><strong>{configuracion.codigoOperacion}</strong></td>
                    <td>{configuracion.descripcion}</td>
                    <td>{configuracion.cantidadCuentasConfiguradas}</td>
                    <td>
                      <span className={`status-pill ${configuracion.activa ? 'is-active' : 'is-inactive'}`}>
                        {configuracion.activa ? 'Activa' : 'Inactiva'}
                      </span>
                    </td>
                    <td>
                      <div className="row-actions">
                        <button className="btn-secondary btn-small" type="button" onClick={() => openExisting(configuracion, true)}>Ver</button>
                        <button className="btn-secondary btn-small" type="button" onClick={() => openExisting(configuracion, false)}>Editar</button>
                        {configuracion.activa ? (
                          <button className="btn-secondary btn-small danger" type="button" onClick={() => handleDeactivate(configuracion)}>Dar de baja</button>
                        ) : (
                          <button className="btn-secondary btn-small" type="button" onClick={() => handleActivate(configuracion)}>Activar</button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {!configuraciones.length && (
                  <tr>
                    <td colSpan="5"><div className="empty-state">No se encontraron configuraciones contables.</div></td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      {showForm && (
        <div className="modal-backdrop">
          <form className="modal-card config-form" onSubmit={handleSubmit}>
            <div className="modal-header">
              <h2>{readOnly ? 'Ver configuracion' : editingConfig ? 'Editar configuracion' : 'Nueva configuracion'}</h2>
              <button className="modal-close" type="button" onClick={closeForm} aria-label="Cerrar">x</button>
            </div>

            <div className="modal-body">
              <div className="filter-grid">
                <label className="form-row">
                  Codigo de operacion
                  <input
                    name="codigoOperacion"
                    value={form.codigoOperacion}
                    onChange={handleHeaderChange}
                    list="tipos-operacion-contable"
                    disabled={readOnly}
                  />
                  <datalist id="tipos-operacion-contable">
                    {tiposOperacion.map((tipo) => <option key={tipo.codigo} value={tipo.codigo}>{tipo.descripcion}</option>)}
                  </datalist>
                </label>
                <label className="form-row">
                  Descripcion
                  <input name="descripcion" value={form.descripcion} onChange={handleHeaderChange} disabled={readOnly} />
                </label>
                {editingConfig && (
                  <label className="checkbox-inline">
                    <input type="checkbox" name="activa" checked={form.activa} onChange={handleHeaderChange} disabled={readOnly} />
                    Activa
                  </label>
                )}
              </div>

              <div className="table-wrapper">
                <table className="data-table asiento-edit-table">
                  <thead>
                    <tr>
                      <th>Concepto</th>
                      <th>Movimiento</th>
                      <th>Cuenta contable</th>
                      <th>Obligatorio</th>
                      <th>Orden</th>
                      {!readOnly && <th>Accion</th>}
                    </tr>
                  </thead>
                  <tbody>
                    {form.detalles.map((detalle, index) => (
                      <tr key={index}>
                        <td>
                          <select value={detalle.concepto} onChange={(event) => handleDetalleChange(index, 'concepto', event.target.value)} disabled={readOnly}>
                            <option value="">Seleccionar</option>
                            {conceptosDisponibles.map((concepto) => (
                              <option key={concepto} value={concepto}>{formatConcepto(concepto)}</option>
                            ))}
                          </select>
                          {detalle.concepto && <span className="table-subtext">{formatConcepto(detalle.concepto)}</span>}
                        </td>
                        <td>
                          <select value={detalle.tipoMovimiento} onChange={(event) => handleDetalleChange(index, 'tipoMovimiento', event.target.value)} disabled={readOnly}>
                            <option value="Debe">Debe</option>
                            <option value="Haber">Haber</option>
                          </select>
                        </td>
                        <td>
                          <select value={detalle.cuentaContableId} onChange={(event) => handleDetalleChange(index, 'cuentaContableId', event.target.value)} disabled={readOnly}>
                            <option value="">Seleccionar</option>
                            {cuentas.map((cuenta) => (
                              <option key={cuenta.id} value={cuenta.id}>{cuenta.codigo} - {cuenta.nombre}</option>
                            ))}
                          </select>
                        </td>
                        <td>
                          <label className="checkbox-inline">
                            <input
                              type="checkbox"
                              checked={detalle.esObligatorio}
                              onChange={(event) => handleDetalleChange(index, 'esObligatorio', event.target.checked)}
                              disabled={readOnly}
                            />
                            {detalle.esObligatorio ? 'Si' : 'No'}
                          </label>
                        </td>
                        <td>
                          <input type="number" min="1" step="1" value={detalle.orden} onChange={(event) => handleDetalleChange(index, 'orden', event.target.value)} disabled={readOnly} />
                        </td>
                        {!readOnly && (
                          <td>
                            <button className="btn-secondary btn-small danger" type="button" onClick={() => removeDetalle(index)} disabled={form.detalles.length <= 2}>
                              Eliminar
                            </button>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {!readOnly && <button className="btn-secondary" type="button" onClick={addDetalle}>Agregar detalle</button>}

              {!readOnly && !!validationErrors.length && (
                <div className="alert-box">
                  {validationErrors.slice(0, 5).map((message) => <p key={message}>{message}</p>)}
                </div>
              )}
              {formError && <p className="form-error">{formError}</p>}
            </div>

            <div className="modal-footer">
              <button className="btn-secondary" type="button" onClick={closeForm}>{readOnly ? 'Cerrar' : 'Cancelar'}</button>
              {!readOnly && (
                <button className="btn-primary" type="submit" disabled={!canSave}>
                  {saving ? 'Guardando...' : 'Guardar'}
                </button>
              )}
            </div>
          </form>
        </div>
      )}
    </div>
  );
};

export default ConfiguracionContablePage;

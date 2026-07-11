import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import asientosContablesService from '../services/asientosContablesService';
import cuentasContablesService from '../services/cuentasContablesService';
import '../contabilidad.css';

const today = new Date().toISOString().slice(0, 10);
const emptyDetalle = () => ({ cuentaContableId: '', descripcion: '', debe: '', haber: '' });

const getApiError = (error) => {
  if (error?.response?.status === 401 || error?.response?.status === 403) return 'No tenes autorizacion para realizar esta accion.';
  if (error?.response?.data?.error) return error.response.data.error;
  if (!error?.response) return 'No se pudo conectar con la API.';
  return 'Ocurrio un error inesperado.';
};

const parseAmount = (value) => {
  if (value === '' || value === null || value === undefined) return 0;
  const parsed = Number(String(value).replace(',', '.'));
  return Number.isFinite(parsed) ? parsed : 0;
};

const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const NuevoAsientoPage = () => {
  const navigate = useNavigate();
  const [cuentas, setCuentas] = useState([]);
  const [form, setForm] = useState({ fecha: today, descripcion: '', detalles: [emptyDetalle(), emptyDetalle()] });
  const [loadingCuentas, setLoadingCuentas] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadCuentas = async () => {
      setLoadingCuentas(true);
      setError('');
      try {
        const data = await cuentasContablesService.getCuentas({ activa: true });
        setCuentas(data);
      } catch (loadError) {
        setError(getApiError(loadError));
      } finally {
        setLoadingCuentas(false);
      }
    };

    loadCuentas();
  }, []);

  const totals = useMemo(() => {
    const totalDebe = form.detalles.reduce((sum, detalle) => sum + parseAmount(detalle.debe), 0);
    const totalHaber = form.detalles.reduce((sum, detalle) => sum + parseAmount(detalle.haber), 0);
    return {
      totalDebe,
      totalHaber,
      diferencia: totalDebe - totalHaber,
      balanced: totalDebe > 0 && Math.abs(totalDebe - totalHaber) < 0.005,
    };
  }, [form.detalles]);

  const validationErrors = useMemo(() => {
    const errors = [];
    if (!form.fecha) errors.push('La fecha es obligatoria.');
    if (!form.descripcion.trim()) errors.push('La descripcion es obligatoria.');
    if (form.detalles.length < 2) errors.push('El asiento debe tener al menos dos renglones.');

    form.detalles.forEach((detalle, index) => {
      const debe = parseAmount(detalle.debe);
      const haber = parseAmount(detalle.haber);
      if (!detalle.cuentaContableId) errors.push(`El renglon ${index + 1} no tiene cuenta contable.`);
      if (debe < 0 || haber < 0) errors.push(`El renglon ${index + 1} tiene importes negativos.`);
      if (debe > 0 && haber > 0) errors.push(`El renglon ${index + 1} tiene Debe y Haber simultaneamente.`);
      if (debe <= 0 && haber <= 0) errors.push(`El renglon ${index + 1} debe tener Debe o Haber mayor a cero.`);
    });

    if (!totals.balanced) errors.push('El asiento esta desbalanceado.');
    return errors;
  }, [form, totals.balanced]);

  const canSave = validationErrors.length === 0 && !saving;

  const handleHeaderChange = (event) => {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
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
    setForm((current) => ({ ...current, detalles: [...current.detalles, emptyDetalle()] }));
  };

  const removeDetalle = (index) => {
    setForm((current) => ({ ...current, detalles: current.detalles.filter((_, detalleIndex) => detalleIndex !== index) }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!canSave) return;

    setSaving(true);
    setError('');

    const payload = {
      fecha: new Date(`${form.fecha}T00:00:00`).toISOString(),
      descripcion: form.descripcion,
      detalles: form.detalles.map((detalle) => ({
        cuentaContableId: Number(detalle.cuentaContableId),
        descripcion: detalle.descripcion || form.descripcion,
        debe: parseAmount(detalle.debe),
        haber: parseAmount(detalle.haber),
      })),
    };

    try {
      const created = await asientosContablesService.createAsiento(payload);
      navigate(`/contabilidad/asientos/${created.id}`);
    } catch (saveError) {
      setError(getApiError(saveError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page-container contabilidad-page">
      <div className="page-header">
        <div>
          <h1>Nuevo Asiento</h1>
          <p className="page-subtitle">Alta manual de asiento contable balanceado.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/contabilidad/asientos">Asientos Contables</Link>
        </div>
      </div>

      <form onSubmit={handleSubmit}>
        <SectionCard title="Encabezado">
          <div className="filter-grid">
            <label className="form-row">
              Fecha
              <input type="date" name="fecha" value={form.fecha} onChange={handleHeaderChange} />
            </label>
            <label className="form-row full-width">
              Descripcion
              <input name="descripcion" value={form.descripcion} onChange={handleHeaderChange} />
            </label>
          </div>
        </SectionCard>

        <SectionCard
          title="Detalle"
          actions={<button className="btn-secondary" type="button" onClick={addDetalle}>Agregar renglon</button>}
        >
          {loadingCuentas && <p className="small-text">Cargando cuentas activas...</p>}
          <div className="table-wrapper">
            <table className="data-table asiento-edit-table">
              <thead>
                <tr>
                  <th>Cuenta contable</th>
                  <th>Descripcion</th>
                  <th>Debe</th>
                  <th>Haber</th>
                  <th>Accion</th>
                </tr>
              </thead>
              <tbody>
                {form.detalles.map((detalle, index) => (
                  <tr key={index}>
                    <td>
                      <select value={detalle.cuentaContableId} onChange={(event) => handleDetalleChange(index, 'cuentaContableId', event.target.value)}>
                        <option value="">Seleccionar</option>
                        {cuentas.map((cuenta) => (
                          <option key={cuenta.id} value={cuenta.id}>{cuenta.codigo} - {cuenta.nombre}</option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input value={detalle.descripcion} onChange={(event) => handleDetalleChange(index, 'descripcion', event.target.value)} />
                    </td>
                    <td>
                      <input type="number" min="0" step="0.01" value={detalle.debe} onChange={(event) => handleDetalleChange(index, 'debe', event.target.value)} />
                    </td>
                    <td>
                      <input type="number" min="0" step="0.01" value={detalle.haber} onChange={(event) => handleDetalleChange(index, 'haber', event.target.value)} />
                    </td>
                    <td>
                      <button className="btn-secondary btn-small danger" type="button" onClick={() => removeDetalle(index)} disabled={form.detalles.length <= 2}>
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="asiento-totals">
            <div><span>Total Debe</span><strong>{formatMoney(totals.totalDebe)}</strong></div>
            <div><span>Total Haber</span><strong>{formatMoney(totals.totalHaber)}</strong></div>
            <div className={totals.balanced ? 'is-balanced' : 'is-unbalanced'}>
              <span>Diferencia</span><strong>{formatMoney(totals.diferencia)}</strong>
            </div>
          </div>

          {!!validationErrors.length && (
            <div className="alert-box">
              {validationErrors.slice(0, 4).map((message) => <p key={message}>{message}</p>)}
            </div>
          )}

          {error && <p className="form-error">{error}</p>}

          <div className="modal-footer">
            <Link className="btn-secondary" to="/contabilidad/asientos">Cancelar</Link>
            <button className="btn-primary" type="submit" disabled={!canSave}>
              {saving ? 'Guardando...' : 'Guardar asiento'}
            </button>
          </div>
        </SectionCard>
      </form>
    </div>
  );
};

export default NuevoAsientoPage;

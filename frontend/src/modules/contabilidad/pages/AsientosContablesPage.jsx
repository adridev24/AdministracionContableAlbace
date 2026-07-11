import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import asientosContablesService from '../services/asientosContablesService';
import cuentasContablesService from '../services/cuentasContablesService';
import '../contabilidad.css';

const getApiError = (error) => {
  if (error?.response?.status === 401 || error?.response?.status === 403) return 'No tenes autorizacion para realizar esta accion.';
  if (error?.response?.data?.error) return error.response.data.error;
  if (!error?.response) return 'No se pudo conectar con la API.';
  return 'Ocurrio un error inesperado.';
};

const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatDate = (value) => (value ? new Date(value).toLocaleDateString('es-AR') : '');

const AsientosContablesPage = () => {
  const [asientos, setAsientos] = useState([]);
  const [cuentas, setCuentas] = useState([]);
  const [filters, setFilters] = useState({
    fechaDesde: '',
    fechaHasta: '',
    descripcion: '',
    cuentaContableId: '',
    tipoAsiento: '',
    estado: '',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const buildQuery = () => ({
    fechaDesde: filters.fechaDesde || undefined,
    fechaHasta: filters.fechaHasta || undefined,
    descripcion: filters.descripcion.trim() || undefined,
    cuentaContableId: filters.cuentaContableId || undefined,
    tipoAsiento: filters.tipoAsiento || undefined,
    estado: filters.estado || undefined,
  });

  const fetchAsientos = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await asientosContablesService.getAsientos(buildQuery());
      setAsientos(data);
    } catch (fetchError) {
      setError(getApiError(fetchError));
    } finally {
      setLoading(false);
    }
  };

  const fetchCuentas = async () => {
    try {
      const data = await cuentasContablesService.getCuentas({});
      setCuentas(data);
    } catch {
      setCuentas([]);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchAsientos();
    fetchCuentas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
  };

  const clearFilters = () => {
    setFilters({ fechaDesde: '', fechaHasta: '', descripcion: '', cuentaContableId: '', tipoAsiento: '', estado: '' });
  };

  const handleReversar = async (asiento) => {
    const confirmed = window.confirm('Confirma la reversion de este asiento?\n\nSe generara un nuevo asiento con los importes invertidos.\nEl asiento original no sera eliminado ni modificado.');
    if (!confirmed) return;

    setError('');
    setSuccess('');
    try {
      const reversion = await asientosContablesService.reversarAsiento(asiento.id);
      setSuccess(`Asiento reversado correctamente. Nuevo asiento #${reversion.id}.`);
      await fetchAsientos();
    } catch (reversalError) {
      setError(getApiError(reversalError));
    }
  };

  return (
    <div className="page-container contabilidad-page">
      <div className="page-header">
        <div>
          <h1>Asientos Contables</h1>
          <p className="page-subtitle">Listado, detalle y reversion de asientos registrados.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/contabilidad">Contabilidad</Link>
          <Link className="btn-secondary" to="/contabilidad/cuentas">Plan de Cuentas</Link>
          <Link className="btn-primary" to="/contabilidad/asientos/nuevo">Nuevo asiento</Link>
        </div>
      </div>

      <SectionCard
        title="Buscar asientos"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={clearFilters}>Limpiar</button>
            <button className="btn-primary" type="button" onClick={fetchAsientos} disabled={loading}>
              {loading ? 'Buscando...' : 'Buscar'}
            </button>
          </>
        )}
      >
        <div className="filter-grid">
          <label className="form-row">
            Fecha desde
            <input type="date" name="fechaDesde" value={filters.fechaDesde} onChange={handleFilterChange} />
          </label>
          <label className="form-row">
            Fecha hasta
            <input type="date" name="fechaHasta" value={filters.fechaHasta} onChange={handleFilterChange} />
          </label>
          <label className="form-row">
            Descripcion
            <input name="descripcion" value={filters.descripcion} onChange={handleFilterChange} />
          </label>
          <label className="form-row">
            Cuenta contable
            <select name="cuentaContableId" value={filters.cuentaContableId} onChange={handleFilterChange}>
              <option value="">Todas</option>
              {cuentas.map((cuenta) => (
                <option key={cuenta.id} value={cuenta.id}>{cuenta.codigo} - {cuenta.nombre}</option>
              ))}
            </select>
          </label>
          <label className="form-row">
            Tipo
            <select name="tipoAsiento" value={filters.tipoAsiento} onChange={handleFilterChange}>
              <option value="">Todos</option>
              <option value="manual">Manual</option>
              <option value="automatico">Automatico</option>
              <option value="reversion">Reversion</option>
            </select>
          </label>
          <label className="form-row">
            Estado
            <select name="estado" value={filters.estado} onChange={handleFilterChange}>
              <option value="">Todos</option>
              <option value="normal">Normal</option>
              <option value="reversado">Reversado</option>
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
                  <th>Numero</th>
                  <th>Fecha</th>
                  <th>Descripcion</th>
                  <th>Tipo</th>
                  <th>Origen</th>
                  <th>Total Debe</th>
                  <th>Total Haber</th>
                  <th>Estado</th>
                  <th>Usuario</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {asientos.map((asiento) => (
                  <tr key={asiento.id}>
                    <td><strong>#{asiento.id}</strong></td>
                    <td>{formatDate(asiento.fecha)}</td>
                    <td>{asiento.descripcion}</td>
                    <td>{asiento.tipo}</td>
                    <td>{asiento.moduloOrigen || 'Manual'}</td>
                    <td>{formatMoney(asiento.totalDebe)}</td>
                    <td>{formatMoney(asiento.totalHaber)}</td>
                    <td>
                      <span className={`status-pill ${asiento.estado === 'Reversado' ? 'is-inactive' : 'is-active'}`}>
                        {asiento.estado}
                      </span>
                    </td>
                    <td>{asiento.usuarioAlta}</td>
                    <td>
                      <div className="row-actions">
                        <Link className="btn-secondary btn-small" to={`/contabilidad/asientos/${asiento.id}`}>Ver</Link>
                        {asiento.tipo !== 'Reversion' && asiento.estado !== 'Reversado' && (
                          <button className="btn-secondary btn-small danger" type="button" onClick={() => handleReversar(asiento)}>
                            Reversar
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {!asientos.length && (
                  <tr>
                    <td colSpan="10"><div className="empty-state">No se encontraron asientos contables.</div></td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>
    </div>
  );
};

export default AsientosContablesPage;

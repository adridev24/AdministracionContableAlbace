import { Fragment, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import ClienteObraSelector from '../../comercial/components/ClienteObraSelector';
import cuentaCorrienteClientesService from '../services/cuentaCorrienteClientesService';
import '../ventas.css';

const initialFilters = {
  clienteExternoId: '',
  obraId: '',
  fechaDesde: '',
  fechaHasta: '',
  moneda: '',
  estadoFactura: '',
};

const estadoLabels = {
  PENDIENTE: 'Pendiente',
  PARCIALMENTE_COBRADA: 'Parcialmente cobrada',
  CANCELADA: 'Cancelada',
};

const estadoClasses = {
  PENDIENTE: 'is-draft',
  PARCIALMENTE_COBRADA: 'is-pending',
  CANCELADA: 'is-active',
};

const getErrorMessage = (error) => {
  if (error?.response?.data?.error) return error.response.data.error;
  if (error?.response?.status === 404) return 'El endpoint de cuenta corriente no esta disponible en la API activa. Reinicia el backend y vuelve a consultar.';
  return 'No fue posible consultar la cuenta corriente.';
};

const money = (value, moneda = '') => {
  const formatted = Number(value || 0).toLocaleString('es-AR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  return moneda ? `${moneda} ${formatted}` : formatted;
};

const dateText = (value) => value ? String(value).slice(0, 10) : '-';

const CuentaCorrienteClientesPage = () => {
  const [filters, setFilters] = useState(initialFilters);
  const [cuenta, setCuenta] = useState(null);
  const [expandedFacturas, setExpandedFacturas] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleClienteObraChange = (values) => {
    setFilters((prev) => ({
      ...prev,
      clienteExternoId: values.clienteExternoId ?? prev.clienteExternoId,
      obraId: values.obraExternoId ?? values.obraExternaId ?? '',
    }));
  };

  const handleClear = () => {
    setFilters(initialFilters);
    setCuenta(null);
    setExpandedFacturas({});
    setError('');
  };

  const handleSearch = async () => {
    if (!filters.clienteExternoId) {
      setError('Selecciona un cliente para consultar la cuenta corriente.');
      return;
    }

    setLoading(true);
    setError('');
    setExpandedFacturas({});
    try {
      const { clienteExternoId, ...query } = filters;
      const response = await cuentaCorrienteClientesService.getCuentaCorriente(clienteExternoId, query);
      setCuenta(response);
    } catch (searchError) {
      setCuenta(null);
      setError(getErrorMessage(searchError));
    } finally {
      setLoading(false);
    }
  };

  const toggleFactura = (ventaId) => {
    setExpandedFacturas((prev) => ({ ...prev, [ventaId]: !prev[ventaId] }));
  };

  return (
    <div className="page-container ventas-page cuenta-corriente-page">
      <div className="page-header">
        <div>
          <h1>Cuenta Corriente de Clientes</h1>
          <p className="page-subtitle">Consulta fiscal de facturas confirmadas y cobranzas aplicadas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
          <Link className="btn-secondary" to="/ventas/cobranzas">Cobranzas</Link>
          <Link className="btn-secondary" to="/">Principal</Link>
        </div>
      </div>

      <SectionCard
        title="Buscar cuenta corriente"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={handleClear} disabled={loading}>Limpiar</button>
            <button className="btn-primary" type="button" onClick={handleSearch} disabled={loading}>
              {loading ? 'Consultando...' : 'Consultar'}
            </button>
          </>
        )}
      >
        <ClienteObraSelector
          clienteExternoId={filters.clienteExternoId}
          obraExternoId={filters.obraId}
          onChange={handleClienteObraChange}
        />
        <div className="form-grid cuenta-corriente-filter-grid">
          <div className="form-field">
            <label>Fecha desde</label>
            <input name="fechaDesde" type="date" value={filters.fechaDesde} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Fecha hasta</label>
            <input name="fechaHasta" type="date" value={filters.fechaHasta} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Moneda</label>
            <select name="moneda" value={filters.moneda} onChange={handleFilterChange}>
              <option value="">Todas</option>
              <option value="ARS">ARS</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="form-field">
            <label>Estado factura</label>
            <select name="estadoFactura" value={filters.estadoFactura} onChange={handleFilterChange}>
              <option value="">Todos</option>
              <option value="PENDIENTE">Pendiente</option>
              <option value="PARCIALMENTE_COBRADA">Parcialmente cobrada</option>
              <option value="CANCELADA">Cancelada</option>
            </select>
          </div>
        </div>
        <p className="hint-text">El rango de fechas limita los movimientos visibles; el saldo final conserva la historia anterior al periodo.</p>
      </SectionCard>

      {error && <p className="form-error">{error}</p>}
      {loading && <LoadingSpinner />}

      {cuenta && !loading && (
        <>
          <SectionCard title={cuenta.clienteNombre || cuenta.clienteId} description="Saldos separados por moneda.">
            {cuenta.saldosPorMoneda.length === 0 ? (
              <p className="empty-state">No hay movimientos para los filtros seleccionados.</p>
            ) : (
              <div className="cc-saldo-grid">
                {cuenta.saldosPorMoneda.map((saldo) => (
                  <div className="cc-saldo-card" key={saldo.monedaCodigo}>
                    <strong className="cc-saldo-moneda">{saldo.monedaCodigo}</strong>
                    <div className="cc-saldo-row">
                      <span>Saldo anterior</span>
                      <b>{money(saldo.saldoAnterior)}</b>
                    </div>
                    <div className="cc-saldo-row">
                      <span>Debe periodo</span>
                      <b>{money(saldo.debePeriodo)}</b>
                    </div>
                    <div className="cc-saldo-row">
                      <span>Haber periodo</span>
                      <b>{money(saldo.haberPeriodo)}</b>
                    </div>
                    <div className="cc-saldo-row is-final">
                      <span>Saldo final</span>
                      <b>{money(saldo.saldoFinal)}</b>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </SectionCard>

          <SectionCard title="Movimientos cronologicos" description={`${cuenta.movimientos.length} movimiento(s).`}>
            <div className="responsive-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Tipo</th>
                    <th>Comprobante / referencia</th>
                    <th>Obra</th>
                    <th>Debe</th>
                    <th>Haber</th>
                    <th>Saldo</th>
                    <th>Moneda</th>
                  </tr>
                </thead>
                <tbody>
                  {cuenta.movimientos.length === 0 && (
                    <tr><td colSpan="8" className="empty-cell">No hay movimientos para mostrar.</td></tr>
                  )}
                  {cuenta.movimientos.map((movimiento) => (
                    <tr key={movimiento.id}>
                      <td>{dateText(movimiento.fecha)}</td>
                      <td>{movimiento.tipoMovimiento}</td>
                      <td>
                        {movimiento.numeroComprobante || movimiento.idOrigen}
                        {movimiento.descripcion && <span className="table-subtext">{movimiento.descripcion}</span>}
                      </td>
                      <td>{movimiento.obraNombre || movimiento.obraId || '-'}</td>
                      <td>{movimiento.debe ? money(movimiento.debe) : '-'}</td>
                      <td>{movimiento.haber ? money(movimiento.haber) : '-'}</td>
                      <td>{money(movimiento.saldoAcumulado)}</td>
                      <td>{movimiento.monedaCodigo}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </SectionCard>

          <SectionCard title="Facturas" description={`${cuenta.facturas.length} factura(s).`}>
            <div className="responsive-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Fecha</th>
                    <th>Comprobante</th>
                    <th>Obra</th>
                    <th>Moneda</th>
                    <th>Total</th>
                    <th>Cobrado</th>
                    <th>Saldo</th>
                    <th>Estado</th>
                    <th>Cobranzas</th>
                  </tr>
                </thead>
                <tbody>
                  {cuenta.facturas.length === 0 && (
                    <tr><td colSpan="9" className="empty-cell">No hay facturas confirmadas para los filtros seleccionados.</td></tr>
                  )}
                  {cuenta.facturas.map((factura) => (
                    <Fragment key={factura.ventaId}>
                      <tr key={factura.ventaId}>
                        <td>{dateText(factura.fecha)}</td>
                        <td>
                          {factura.numeroComprobante}
                          <span className="table-subtext">{factura.tipoComprobante}</span>
                        </td>
                        <td>{factura.obraNombre || factura.obraId || '-'}</td>
                        <td>{factura.monedaCodigo}</td>
                        <td>{money(factura.totalFactura)}</td>
                        <td>{money(factura.totalCobrado)}</td>
                        <td>{money(factura.saldo)}</td>
                        <td>
                          <span className={`status-pill ${estadoClasses[factura.estadoCobranza] || 'is-pending'}`}>
                            {estadoLabels[factura.estadoCobranza] || factura.estadoCobranza}
                          </span>
                        </td>
                        <td>
                          <button className="btn-secondary btn-small" type="button" onClick={() => toggleFactura(factura.ventaId)}>
                            {expandedFacturas[factura.ventaId] ? 'Ocultar' : 'Ver'}
                          </button>
                        </td>
                      </tr>
                      {expandedFacturas[factura.ventaId] && (
                        <tr key={`${factura.ventaId}-cobranzas`} className="cc-detail-row">
                          <td colSpan="9">
                            {factura.cobranzas.length === 0 ? (
                              <span className="table-subtext">Sin cobranzas aplicadas.</span>
                            ) : (
                              <div className="cc-cobranzas-list">
                                {factura.cobranzas.map((cobranza) => (
                                  <span key={`${factura.ventaId}-${cobranza.cobranzaId}`}>
                                    Cobranza {cobranza.cobranzaId} · {dateText(cobranza.fecha)} · {money(cobranza.importeAplicado, factura.monedaCodigo)} · {cobranza.estadoCobranza}
                                  </span>
                                ))}
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </SectionCard>
        </>
      )}
    </div>
  );
};

export default CuentaCorrienteClientesPage;

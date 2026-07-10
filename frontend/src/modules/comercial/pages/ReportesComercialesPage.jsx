import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import externalDataService from '../services/externalDataService';
import reportesComercialesService from '../services/reportesComercialesService';
import '../comercial.css';

const formatMoney = (value, monedaCodigo = 'ARS') =>
  `${monedaCodigo} ${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const toDateInputValue = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const formatDate = (value) => {
  if (!value) return '-';
  const [year, month, day] = String(value).slice(0, 10).split('-');
  return year && month && day ? `${day}/${month}/${year}` : '-';
};

const getDefaultPeriod = () => {
  const now = new Date();
  const from = new Date(now.getFullYear(), now.getMonth(), 1);
  const to = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  return {
    desde: toDateInputValue(from),
    hasta: toDateInputValue(to),
    via: 'Todos',
  };
};

const ReportesComercialesPage = () => {
  const [period, setPeriod] = useState(getDefaultPeriod);
  const [resumen, setResumen] = useState(null);
  const [clientNames, setClientNames] = useState({});
  const [obraNames, setObraNames] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const loadResumen = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await reportesComercialesService.getResumen(period);
      setResumen(data);
    } catch {
      setError('No se pudo cargar el resumen comercial.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadResumen();
  }, []);

  useEffect(() => {
    if (!resumen) return;

    const clienteIds = [
      ...(resumen.clientesConDeuda ?? []).map((cliente) => cliente.clienteExternoId),
      ...(resumen.proximosVencimientos ?? []).map((cuota) => cuota.clienteExternoId),
    ].filter((value, index, items) => value && items.indexOf(value) === index);

    clienteIds.forEach((clienteId) => {
      if (clientNames[clienteId]) return;

      externalDataService.getClientById(Number(clienteId))
        .then((client) => {
          if (client?.nombreCliente) {
            setClientNames((prev) => ({ ...prev, [clienteId]: client.nombreCliente }));
          }
        })
        .catch(() => {});
    });

    const obraIds = (resumen.proximosVencimientos ?? [])
      .map((cuota) => cuota.obraExternaId)
      .filter((value, index, items) => value && items.indexOf(value) === index);

    obraIds.forEach((obraId) => {
      if (obraNames[obraId]) return;

      externalDataService.getObraById(Number(obraId))
        .then((obra) => {
          if (obra?.nombreObra) {
            setObraNames((prev) => ({ ...prev, [obraId]: obra.nombreObra }));
          }
        })
        .catch(() => {});
    });
  }, [clientNames, obraNames, resumen]);

  const kpis = useMemo(() => {
    if (!resumen) return [];

    return [
      { label: 'Saldo de deuda', value: resumen.saldoTotalClientes, hint: 'Saldo pendiente activo' },
      { label: 'A cobrar en el periodo', value: resumen.totalPorCobrarPeriodo, hint: `${resumen.cuotasPendientesPeriodo} cuotas` },
      { label: 'Cobrado en el periodo', value: resumen.totalCobradoPeriodo, hint: 'Pagos registrados' },
      { label: 'Vencido pendiente', value: resumen.totalVencido, hint: `${resumen.cuotasVencidas} cuotas vencidas` },
    ];
  }, [resumen]);

  const handlePeriodChange = (event) => {
    const { name, value } = event.target;
    setPeriod((prev) => ({ ...prev, [name]: value }));
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Reportes comerciales</h1>
          <p className="page-subtitle">Indicadores rapidos para deuda de clientes, vencimientos y cobranzas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/comercial">Volver a acuerdos</Link>
        </div>
      </div>

      <SectionCard
        title="Periodo de consulta"
        description="Por defecto se muestra el mes actual."
        actions={(
          <button className="btn-primary" type="button" onClick={loadResumen} disabled={loading}>
            {loading ? 'Actualizando...' : 'Actualizar'}
          </button>
        )}
      >
        <div className="report-filter-grid">
          <div className="form-field">
            <label>Desde</label>
            <input type="date" name="desde" value={period.desde} onChange={handlePeriodChange} />
          </div>
          <div className="form-field">
            <label>Hasta</label>
            <input type="date" name="hasta" value={period.hasta} onChange={handlePeriodChange} />
          </div>
          <div className="form-field">
            <label>Via</label>
            <select name="via" value={period.via} onChange={handlePeriodChange}>
              <option value="Todos">Todos</option>
              <option value="Via1">Via1</option>
              <option value="Via2">Via2</option>
            </select>
          </div>
        </div>
      </SectionCard>

      {error && <p className="form-error">{error}</p>}
      {loading && !resumen ? (
        <LoadingSpinner />
      ) : resumen && (
        <>
          <div className="report-kpi-grid">
            {kpis.map((kpi) => (
              <div className="report-kpi" key={kpi.label}>
                <span>{kpi.label}</span>
                <strong>{formatMoney(kpi.value ?? 0)}</strong>
                <small>{kpi.hint}</small>
              </div>
            ))}
          </div>

          <SectionCard title="Clientes con mayor deuda" description="Ranking para priorizar seguimiento comercial.">
            {(resumen.clientesConDeuda ?? []).length > 0 ? (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Cliente</th>
                      <th>Acuerdos</th>
                      <th>Total acuerdos</th>
                      <th>Total pagado</th>
                      <th>Saldo pendiente</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resumen.clientesConDeuda.map((cliente) => (
                      <tr key={cliente.clienteExternoId}>
                        <td><strong>{clientNames[cliente.clienteExternoId] || cliente.clienteExternoId}</strong></td>
                        <td>{cliente.acuerdosActivos}</td>
                        <td>{formatMoney(cliente.totalAcordado)}</td>
                        <td>{formatMoney(cliente.totalPagado)}</td>
                        <td>{formatMoney(cliente.saldoPendiente)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state empty-state-box">
                <strong>Sin deuda activa</strong>
                <p>No hay clientes con saldo pendiente en acuerdos activos.</p>
              </div>
            )}
          </SectionCard>

          <SectionCard title="Proximos vencimientos" description="Cuotas pendientes ordenadas por fecha de vencimiento.">
            {(resumen.proximosVencimientos ?? []).length > 0 ? (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Vencimiento</th>
                      <th>Acuerdo</th>
                      <th>Cliente</th>
                      <th>Obra</th>
                      <th>Estado</th>
                      <th>Saldo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resumen.proximosVencimientos.map((cuota) => (
                      <tr key={cuota.cuotaId}>
                        <td>{formatDate(cuota.fechaVencimiento)}</td>
                        <td>{cuota.numeroAcuerdo}</td>
                        <td>{clientNames[cuota.clienteExternoId] || cuota.clienteExternoId}</td>
                        <td>{obraNames[cuota.obraExternaId] || cuota.obraExternaId}</td>
                        <td>{cuota.estado}</td>
                        <td>{formatMoney(cuota.saldoPendiente, cuota.monedaCodigo || 'ARS')}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty-state empty-state-box">
                <strong>Sin vencimientos pendientes</strong>
                <p>No hay cuotas futuras con saldo pendiente.</p>
              </div>
            )}
          </SectionCard>
        </>
      )}
    </div>
  );
};

export default ReportesComercialesPage;

import { defaultReportFilters, generalReportFilters, reportQuery, appliedReportFilters, reportCaption } from '../services/reportFilters.js';
import { formatCalendarDate } from '../../../shared/utils/calendarDate';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import externalDataService from '../services/externalDataService';
import reportesComercialesService from '../services/reportesComercialesService';
import '../comercial.css';

const formatMoney = (value, monedaCodigo = 'ARS') =>
  `${monedaCodigo} ${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const ReportesComercialesPage = () => {
  const requestSequence = useRef(0);
  const [period, setPeriod] = useState(defaultReportFilters);
  const [applied, setApplied] = useState(null);
  const [resumen, setResumen] = useState(null);
  const [clientNames, setClientNames] = useState({});
  const [obraNames, setObraNames] = useState({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const loadResumen = async (filters = period) => {
    const snapshot = { ...filters };
    try {
      reportQuery(snapshot);
    } catch (validationError) {
      setError(validationError.message);
      return;
    }
    const sequence = ++requestSequence.current;
    setLoading(true);
    setError('');
    try {
      const data = await reportesComercialesService.getResumen(snapshot);
      if (sequence !== requestSequence.current) return;
      setResumen(data);
      setApplied(appliedReportFilters(snapshot, data));
    } catch {
      if (sequence === requestSequence.current) setError('No se pudo cargar el resumen comercial.');
    } finally {
      if (sequence === requestSequence.current) setLoading(false);
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

  const totalGroups = useMemo(() => {
    if (!resumen) return [];

    const totales = resumen.totalesPorMoneda?.length
      ? resumen.totalesPorMoneda
      : [{
          monedaCodigo: 'ARS',
          totalAcordadoActivo: resumen.totalAcordadoActivo,
          saldoTotalClientes: resumen.saldoTotalClientes,
          totalPorCobrarPeriodo: resumen.totalPorCobrarPeriodo,
          totalCobradoPeriodo: resumen.totalCobradoPeriodo,
          totalVencido: resumen.totalVencido,
        }];

    const isPeriod = resumen.alcance === 'Periodo';
    return totales.map((total) => ({
      monedaCodigo: total.monedaCodigo || 'ARS',
      items: [
        { label: isPeriod ? 'Acordado vigente (situación actual)' : 'Acordado vigente', value: total.totalAcordadoActivo, hint: 'Acuerdos vigentes' },
        { label: isPeriod ? 'Saldo total pendiente (situación actual)' : 'Saldo total pendiente', value: total.saldoTotalClientes, hint: 'Saldo pendiente activo' },
        { label: isPeriod ? 'Pendiente con vencimiento en el período' : 'Pendiente en obligaciones', value: total.totalPorCobrarPeriodo, hint: `${total.cuotasPendientesPeriodo ?? 0} obligaciones` },
        { label: isPeriod ? 'Cobrado en el período' : 'Cobrado acumulado', value: total.totalCobradoPeriodo, hint: isPeriod ? 'Rango consultado' : 'Sin límite de fecha' },
        { label: 'Vencido actualmente', value: total.totalVencido, hint: `${total.cuotasVencidas ?? 0} obligaciones` },
      ],
    }));
  }, [resumen]);

  const handlePeriodChange = (event) => {
    const { name, value } = event.target;
    setPeriod((prev) => name === 'alcance' && value === 'General'
      ? generalReportFilters(prev)
      : { ...prev, [name]: value });
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
        title="Alcance de consulta"
        description="Los filtros se aplican al pulsar Actualizar."
      >
        <div className="report-filter-grid report-query-grid">
          <div className="form-field report-query-alcance">
            <label htmlFor="report-alcance">Alcance</label>
            <select id="report-alcance" name="alcance" value={period.alcance} onChange={handlePeriodChange}>
              <option value="General">Totales generales</option>
              <option value="Periodo">Por período</option>
            </select>
          </div>
          <div className="form-field report-query-desde">
            <label htmlFor="report-desde">Desde</label>
            <input type="date" id="report-desde" disabled={period.alcance !== 'Periodo'} required={period.alcance === 'Periodo'} name="desde" value={period.desde} onChange={handlePeriodChange} />
          </div>
          <div className="form-field report-query-hasta">
            <label htmlFor="report-hasta">Hasta</label>
            <input type="date" id="report-hasta" disabled={period.alcance !== 'Periodo'} required={period.alcance === 'Periodo'} name="hasta" value={period.hasta} onChange={handlePeriodChange} />
          </div>
          <div className="form-field report-query-via">
            <label htmlFor="report-via">Vía</label>
            <select id="report-via" name="via" value={period.via} onChange={handlePeriodChange}>
              <option value="Todos">Todos</option>
              <option value="Via1">Vía 1</option>
              <option value="Via2">Vía 2</option>
            </select>
          </div>
        </div>
        <div className="report-query-actions">
          {(period.alcance === 'Periodo' || applied?.alcance === 'Periodo') && (
            <button className="btn-secondary" type="button" disabled={loading} onClick={() => {
              const general = generalReportFilters(period);
              setPeriod(general);
              loadResumen(general);
            }}>Ver totales generales</button>
          )}
          <button className="btn-primary" type="button" onClick={() => loadResumen()} disabled={loading}>
            {loading ? 'Actualizando...' : 'Actualizar'}
          </button>
        </div>
      </SectionCard>

      {error && <p className="form-error">{error}</p>}
      {loading && !resumen ? (
        <LoadingSpinner />
      ) : resumen && (
        <>
          <p aria-live="polite">{reportCaption(applied)}</p>
          <p>El saldo total corresponde al acuerdo completo; el pendiente en obligaciones corresponde a anticipos y cuotas.
            Pueden diferir cuando existen pagos a cuenta todavía no aplicados.
            La consulta por período no representa un saldo histórico al cierre.</p>
          <div className="report-currency-groups">
            {totalGroups.map((group) => (
              <div className="report-currency-group" key={group.monedaCodigo}>
                <div className="report-currency-label">
                  <span>Moneda</span>
                  <strong>{group.monedaCodigo}</strong>
                </div>
                <div className="report-kpi-grid">
                  {group.items.map((kpi) => (
                    <div className="report-kpi" key={`${group.monedaCodigo}-${kpi.label}`}>
                      <span>{kpi.label}</span>
                      <strong>{formatMoney(kpi.value ?? 0, group.monedaCodigo)}</strong>
                      <small>{kpi.hint}</small>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>

          <SectionCard title="Clientes con mayor deuda actual" description="Hasta diez clientes por moneda. Situación actual, no detalle conciliatorio del período.">
            {(resumen.clientesConDeuda ?? []).length > 0 ? (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Cliente</th>
                      <th>Moneda</th>
                      <th>Acuerdos</th>
                      <th>Total acuerdos</th>
                      <th>Total pagado</th>
                      <th>Saldo pendiente</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resumen.clientesConDeuda.map((cliente) => (
                      <tr key={`${cliente.clienteExternoId}-${cliente.monedaCodigo || 'ARS'}`}>
                        <td><strong>{clientNames[cliente.clienteExternoId] || cliente.clienteExternoId}</strong></td>
                        <td>{cliente.monedaCodigo || 'ARS'}</td>
                        <td>{cliente.acuerdosActivos}</td>
                        <td>{formatMoney(cliente.totalAcordado, cliente.monedaCodigo || 'ARS')}</td>
                        <td>{formatMoney(cliente.totalPagado, cliente.monedaCodigo || 'ARS')}</td>
                        <td>{formatMoney(cliente.saldoPendiente, cliente.monedaCodigo || 'ARS')}</td>
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

          <SectionCard title="Próximos vencimientos actuales" description="Hasta diez obligaciones desde hoy, ordenadas por vencimiento. Situación actual, no detalle del período.">
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
                        <td>{formatCalendarDate(cuota.fechaVencimiento)}</td>
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

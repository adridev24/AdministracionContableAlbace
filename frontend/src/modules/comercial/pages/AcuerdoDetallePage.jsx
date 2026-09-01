import { useEffect, useMemo, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import EstadoComercialResumen from '../components/EstadoComercialResumen';
import PlanPagoForm from '../components/PlanPagoForm';
import PlanPagoEditor from '../components/PlanPagoEditor';
import CuotasComercialesTable from '../components/CuotasComercialesTable';
import AjusteCuotaModal from '../components/AjusteCuotaModal';
import AgregarCuotaModal from '../components/AgregarCuotaModal';
import SituacionVia1Panel from '../components/SituacionVia1Panel';
import useAcuerdoDetalle from '../hooks/useAcuerdoDetalle';
import acuerdosService from '../services/acuerdosService';
import externalDataService from '../services/externalDataService';
import '../comercial.css';

const currency = (value, moneda = 'ARS') =>
  `${moneda ? `${moneda} ` : ''}${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const formatDate = (value) => {
  if (!value) return '-';
  const [year, month, day] = String(value).slice(0, 10).split('-');
  return year && month && day ? `${day}/${month}/${year}` : '-';
};

const summarizeVias = (vias) => {
  const totals = vias.reduce((acc, via) => {
    acc[via.monedaCodigo] = (acc[via.monedaCodigo] || 0) + Number(via.montoActual || 0);
    return acc;
  }, {});

  return Object.entries(totals)
    .map(([moneda, total]) => currency(total, moneda))
    .join(' / ');
};

const getApiError = (error, fallback) => error?.response?.data?.error || fallback;

const AcuerdoDetallePage = () => {
  const { id } = useParams();
  const { detalle, loading, error, setDetalle } = useAcuerdoDetalle(id);
  const [selectedViaId, setSelectedViaId] = useState(null);
  const [clienteNombre, setClienteNombre] = useState('');
  const [obraNombre, setObraNombre] = useState('');
  const [planLoading, setPlanLoading] = useState(false);
  const [planError, setPlanError] = useState('');
  const [success, setSuccess] = useState('');
  const [ajustarModalOpen, setAjustarModalOpen] = useState(false);
  const [selectedCuota, setSelectedCuota] = useState(null);
  const [agregarModalOpen, setAgregarModalOpen] = useState(false);
  const [modalLoading, setModalLoading] = useState(false);
  const [modalError, setModalError] = useState('');
  const [montoForm, setMontoForm] = useState({
    nuevoMonto: '',
    motivo: '',
    refinanciarCuotasPendientes: true
  });
  const [montoLoading, setMontoLoading] = useState(false);
  const [hitoForm, setHitoForm] = useState({ descripcion: '', importeEstimado: '', fechaReferencia: '', observaciones: '' });
  const [hitoLoading, setHitoLoading] = useState(false);
  const [aprobarLoading, setAprobarLoading] = useState(false);

  const vias = detalle?.vias ?? [];
  const selectedVia = vias.find((via) => via.id === selectedViaId) ?? vias[0];
  const isBorrador = detalle?.estado === 'Borrador' && selectedVia?.estado === 'Borrador';
  const canAgregarCuota = selectedVia?.planPago && ['Aprobado', 'EnCurso'].includes(selectedVia?.estado);
  const viasPlanificadasSinPlan = vias.filter((via) => via.modalidadCobro === 'Planificada' && !via.planPago);

  useEffect(() => {
    if (vias.length && !selectedViaId) {
      setSelectedViaId(vias[0].id);
    }
  }, [selectedViaId, vias]);

  useEffect(() => {
    if (!detalle) return;

    const clienteId = Number(detalle.clienteExternoId);
    const obraId = Number(detalle.obraExternaId);

    if (!Number.isNaN(clienteId)) {
      externalDataService.getClientById(clienteId)
        .then((client) => setClienteNombre(client?.nombreCliente || ''))
        .catch(() => setClienteNombre(''));
    }

    if (!Number.isNaN(obraId)) {
      externalDataService.getObraById(obraId)
        .then((obra) => setObraNombre(obra?.nombreObra || ''))
        .catch(() => setObraNombre(''));
    }
  }, [detalle]);

  useEffect(() => {
    if (selectedVia) {
      setMontoForm((prev) => ({ ...prev, nuevoMonto: selectedVia.montoActual || '' }));
    }
  }, [selectedVia]);

  const estadoComercial = useMemo(() => {
    if (!selectedVia) return null;
    return {
      totalPrometido: selectedVia.montoActual,
      totalPagado: selectedVia.totalPagado,
      saldoRestante: selectedVia.saldoPendiente,
    };
  }, [selectedVia]);

  const refreshDetalle = async () => {
    const updatedDetalle = await acuerdosService.getAcuerdoDetalle(id);
    setDetalle(updatedDetalle);
  };

  const handleCreatePlan = async (payload) => {
    if (!selectedVia) return;
    setPlanLoading(true);
    setPlanError('');
    setSuccess('');
    try {
      await acuerdosService.crearPlanPagoVia(selectedVia.id, payload);
      await refreshDetalle();
      setSuccess('Plan de pago generado correctamente.');
    } catch {
      setPlanError('No se pudo generar el plan. Revisa los datos y vuelve a intentar.');
    } finally {
      setPlanLoading(false);
    }
  };

  const handleSavePlan = async (payload) => {
    if (!selectedVia) return;
    setPlanLoading(true);
    setPlanError('');
    setSuccess('');
    try {
      await acuerdosService.actualizarPlanPagoVia(selectedVia.id, payload);
      await refreshDetalle();
      setSuccess('Plan actualizado correctamente.');
    } catch (err) {
      setPlanError(getApiError(err, 'No se pudo actualizar el plan. Revisa los valores y vuelve a intentar.'));
    } finally {
      setPlanLoading(false);
    }
  };

  const openAjustarCuota = (cuota) => {
    setSelectedCuota(cuota);
    setModalError('');
    setAjustarModalOpen(true);
  };

  const handleAjustarCuota = async (payload) => {
    setModalLoading(true);
    setModalError('');
    setSuccess('');
    try {
      await acuerdosService.ajustarCuota(selectedCuota.id, payload);
      await refreshDetalle();
      setSuccess('Ajuste de cuota registrado correctamente.');
      setAjustarModalOpen(false);
      setSelectedCuota(null);
    } catch {
      setModalError('No se pudo guardar el ajuste. Revisa los datos e intenta nuevamente.');
    } finally {
      setModalLoading(false);
    }
  };

  const handleAgregarCuota = async (payload) => {
    if (!selectedVia?.planPago) return;
    setModalLoading(true);
    setModalError('');
    setSuccess('');
    try {
      await acuerdosService.agregarCuotaAjuste(selectedVia.planPago.id, payload);
      await refreshDetalle();
      setSuccess('Cuota adicional agregada correctamente.');
      setAgregarModalOpen(false);
    } catch (err) {
      setModalError(getApiError(err, 'No se pudo agregar la cuota. Revisa los datos e intenta nuevamente.'));
    } finally {
      setModalLoading(false);
    }
  };

  const handleModificarMonto = async (event) => {
    event.preventDefault();
    if (!selectedVia) return;

    setMontoLoading(true);
    setSuccess('');
    setPlanError('');
    try {
      await acuerdosService.modificarMontoVia(selectedVia.id, {
        nuevoMonto: Number(montoForm.nuevoMonto),
        refinanciarCuotasPendientes: montoForm.refinanciarCuotasPendientes,
        motivo: montoForm.motivo,
      });
      await refreshDetalle();
      setSuccess('Monto de la via actualizado correctamente.');
      setMontoForm((prev) => ({ ...prev, motivo: '' }));
    } catch (err) {
      setPlanError(getApiError(err, 'No se pudo modificar el monto de la via.'));
    } finally {
      setMontoLoading(false);
    }
  };

  const handleCrearHito = async (event) => {
    event.preventDefault();
    if (!selectedVia) return;

    setHitoLoading(true);
    setPlanError('');
    setSuccess('');
    try {
      const nuevoHito = await acuerdosService.crearHitoVia(selectedVia.id, {
        descripcion: hitoForm.descripcion,
        importeEstimado: Number(hitoForm.importeEstimado || 0),
        fechaReferencia: new Date(hitoForm.fechaReferencia).toISOString(),
        observaciones: hitoForm.observaciones
      });
      setDetalle((prev) => ({
        ...prev,
        vias: prev.vias.map((via) => (
          via.id === selectedVia.id
            ? { ...via, hitos: [...(via.hitos || []), nuevoHito].sort((a, b) => new Date(a.fechaReferencia) - new Date(b.fechaReferencia)) }
            : via
        ))
      }));
      await refreshDetalle();
      setSuccess('Hito comercial registrado correctamente.');
      setHitoForm({ descripcion: '', importeEstimado: '', fechaReferencia: '', observaciones: '' });
    } catch (err) {
      setPlanError(getApiError(err, 'No se pudo registrar el hito comercial.'));
    } finally {
      setHitoLoading(false);
    }
  };

  const handleAprobarAcuerdo = async () => {
    if (viasPlanificadasSinPlan.length) {
      setPlanError(`No se puede aprobar el acuerdo. Cree el plan de pago para: ${viasPlanificadasSinPlan.map((via) => via.viaOperacion).join(', ')}.`);
      return;
    }

    if (!window.confirm('¿Aprobar este acuerdo comercial?')) return;

    setAprobarLoading(true);
    setPlanError('');
    setSuccess('');
    try {
      await acuerdosService.aprobarAcuerdo(id);
      await refreshDetalle();
      setSuccess('Acuerdo aprobado correctamente.');
    } catch (err) {
      setPlanError(getApiError(err, 'No se pudo aprobar el acuerdo.'));
    } finally {
      setAprobarLoading(false);
    }
  };

  if (loading) return <LoadingSpinner />;
  if (error) return <div className="page-container"><p className="form-error">{error}</p></div>;
  if (!detalle) return <div className="page-container"><p className="empty-state">Acuerdo no encontrado.</p></div>;

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Detalle del acuerdo {detalle.numeroAcuerdo}</h1>
          <p className="page-subtitle">Cabecera comercial con condiciones economicas administradas por via.</p>
        </div>
        <div className="page-actions">
          {detalle.estado === 'Borrador' && (
            <button className="btn-primary" type="button" onClick={handleAprobarAcuerdo} disabled={aprobarLoading}>
              {aprobarLoading ? 'Aprobando...' : 'Aprobar acuerdo'}
            </button>
          )}
          <Link className="btn-secondary" to="/comercial">Volver a acuerdos</Link>
        </div>
      </div>

      {success && <p className="form-success">{success}</p>}
      {planError && <p className="form-error">{planError}</p>}
      {detalle.estado === 'Borrador' && viasPlanificadasSinPlan.length > 0 && (
        <p className="form-error">
          Para aprobar, primero cree el plan de pago en: {viasPlanificadasSinPlan.map((via) => via.viaOperacion).join(', ')}.
        </p>
      )}

      <section className="detail-hero">
        <div>
          <span className="eyebrow">Acuerdo comercial</span>
          <h2>{clienteNombre || detalle.clienteExternoId}</h2>
          <p>{obraNombre || detalle.obraExternaId}</p>
        </div>
        <div className="detail-hero-facts">
          <div><span>Estado</span><strong>{detalle.estado}</strong></div>
          <div><span>Vias</span><strong>{vias.length}</strong></div>
          <div><span>Monto vigente</span><strong>{summarizeVias(vias) || currency(detalle.montoTotal)}</strong></div>
          <div><span>Fecha</span><strong>{formatDate(detalle.fechaAcuerdo)}</strong></div>
        </div>
      </section>

      <div className="via-tabs">
        {vias.map((via) => (
          <button
            key={via.id}
            className={via.id === selectedVia?.id ? 'active' : ''}
            type="button"
            onClick={() => setSelectedViaId(via.id)}
          >
            {via.viaOperacion} - {via.monedaCodigo}
          </button>
        ))}
      </div>

      {selectedVia && (
        <>
          <SectionCard title={`${selectedVia.viaOperacion} - ${selectedVia.monedaCodigo}`} description="Estado economico de la via seleccionada.">
            <div className="info-grid">
              <div><strong>Monto original</strong><p>{currency(selectedVia.montoOriginal, selectedVia.monedaCodigo)}</p></div>
              <div><strong>Monto actual</strong><p>{currency(selectedVia.montoActual, selectedVia.monedaCodigo)}</p></div>
              <div><strong>Total pagado</strong><p>{currency(selectedVia.totalPagado, selectedVia.monedaCodigo)}</p></div>
              <div><strong>Saldo pendiente</strong><p>{currency(selectedVia.saldoPendiente, selectedVia.monedaCodigo)}</p></div>
              <div><strong>Modalidad</strong><p>{selectedVia.modalidadCobro}</p></div>
              <div><strong>Estado</strong><p>{selectedVia.estado}</p></div>
              <div><strong>Observaciones</strong><p>{selectedVia.observaciones || '-'}</p></div>
            </div>
          </SectionCard>

          <SectionCard title="Estado comercial por via" description="Suma pagada y deuda restante de esta via.">
            <EstadoComercialResumen estado={estadoComercial} />
          </SectionCard>

          {selectedVia.viaOperacion === 'Via1' && (
            <SectionCard title="Situacion Via 1" description="Resumen de plan, facturacion y cobranzas confirmadas de la via.">
              <SituacionVia1Panel acuerdoId={Number(id)} enabled={selectedVia.viaOperacion === 'Via1'} />
            </SectionCard>
          )}

          {isBorrador && (
            <SectionCard title="Modificar monto de la via" description="Antes de aprobar, puede recalcular las cuotas pendientes.">
              <form className="report-filter-grid" onSubmit={handleModificarMonto}>
                <div className="form-field">
                  <label>Nuevo monto</label>
                  <input type="number" min="0.01" step="0.01" value={montoForm.nuevoMonto} onChange={(e) => setMontoForm((prev) => ({ ...prev, nuevoMonto: e.target.value }))} required />
                </div>
                <div className="form-field full-width">
                  <label>Motivo</label>
                  <input value={montoForm.motivo} onChange={(e) => setMontoForm((prev) => ({ ...prev, motivo: e.target.value }))} required />
                </div>
                <label className="via-toggle">
                  <input type="checkbox" checked={montoForm.refinanciarCuotasPendientes} onChange={(e) => setMontoForm((prev) => ({ ...prev, refinanciarCuotasPendientes: e.target.checked }))} />
                  Refinanciar cuotas pendientes
                </label>
                <div className="form-footer full-width">
                  <button className="btn-primary" type="submit" disabled={montoLoading}>
                    {montoLoading ? 'Actualizando...' : 'Modificar monto'}
                  </button>
                </div>
              </form>
            </SectionCard>
          )}

          {!selectedVia.planPago ? (
            <SectionCard title="Generar plan de pago" description={`Esta creando el plan de pago para ${selectedVia.viaOperacion} - ${selectedVia.monedaCodigo}.`}>
              {isBorrador ? (
                <PlanPagoForm onSubmit={handleCreatePlan} loading={planLoading} />
              ) : (
                <p className="empty-state">El acuerdo ya fue aprobado. No se puede crear un plan base nuevo.</p>
              )}
            </SectionCard>
          ) : (
            <SectionCard
              title="Plan de pago"
              description={isBorrador
                ? `Antes de aprobar puede ajustar anticipo y cuotas hasta que el total coincida con el monto de la via.`
                : `Plan base asociado a ${selectedVia.viaOperacion} - ${selectedVia.monedaCodigo}.`}
              actions={canAgregarCuota && (
                <button className="btn-primary" type="button" onClick={() => setAgregarModalOpen(true)}>
                  Agregar cuota
                </button>
              )}
            >
              <PlanPagoEditor
                planPago={selectedVia.planPago}
                viaMonto={selectedVia.montoActual}
                monedaCodigo={selectedVia.monedaCodigo}
                onSave={handleSavePlan}
                loading={planLoading}
                error={planError}
                readOnly={!isBorrador}
              />
              <CuotasComercialesTable cuotas={selectedVia.planPago.cuotas} />
            </SectionCard>
          )}

          <SectionCard title="Pagos aplicados" description="Pagos comerciales registrados para esta via.">
            {selectedVia.viaOperacion === 'Via1' && (
              <div className="alert-box">Los pagos de Via1 se registran desde el modulo Ventas.</div>
            )}
            {selectedVia.pagos?.length ? (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Fecha pago</th>
                      <th>Importe total</th>
                      <th>Medio</th>
                      <th>Estado</th>
                      <th>Aplicado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedVia.pagos.map((pago) => (
                      <tr key={pago.id}>
                        <td>{formatDate(pago.fechaPago)}</td>
                        <td>{currency(pago.importeTotal, pago.monedaCodigo)}</td>
                        <td>{pago.medioPago}</td>
                        <td>{pago.origenPago} / {pago.tipoImputacion}</td>
                        <td>{currency(pago.aplicaciones.reduce((sum, app) => sum + app.importeAplicado, 0), pago.monedaCodigo)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="empty-state">Aun no se registran pagos para esta via.</p>
            )}
          </SectionCard>

          {selectedVia.modalidadCobro === 'Abierta' && (
            <SectionCard title="Hitos comerciales" description="Clasificadores opcionales para pagos de Via2.">
              <form className="report-filter-grid" onSubmit={handleCrearHito}>
                <div className="form-field">
                  <label>Descripcion</label>
                  <input value={hitoForm.descripcion} onChange={(e) => setHitoForm((prev) => ({ ...prev, descripcion: e.target.value }))} required />
                </div>
                <div className="form-field">
                  <label>Importe estimado</label>
                  <input type="number" min="0" step="0.01" value={hitoForm.importeEstimado} onChange={(e) => setHitoForm((prev) => ({ ...prev, importeEstimado: e.target.value }))} />
                </div>
                <div className="form-field">
                  <label>Fecha referencia</label>
                  <input type="date" value={hitoForm.fechaReferencia} onChange={(e) => setHitoForm((prev) => ({ ...prev, fechaReferencia: e.target.value }))} required />
                </div>
                <div className="form-field full-width">
                  <label>Observaciones</label>
                  <input value={hitoForm.observaciones} onChange={(e) => setHitoForm((prev) => ({ ...prev, observaciones: e.target.value }))} />
                </div>
                <div className="form-footer full-width">
                  <button className="btn-primary" type="submit" disabled={hitoLoading}>{hitoLoading ? 'Guardando...' : 'Agregar hito'}</button>
                </div>
              </form>

              {selectedVia.hitos?.length ? (
                <div className="table-wrapper">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Fecha</th>
                        <th>Descripcion</th>
                        <th>Estimado</th>
                        <th>Aplicado</th>
                        <th>Estado</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedVia.hitos.map((hito) => (
                        <tr key={hito.id}>
                          <td>{formatDate(hito.fechaReferencia)}</td>
                          <td>{hito.descripcion}</td>
                          <td>{currency(hito.importeEstimado, selectedVia.monedaCodigo)}</td>
                          <td>{currency(hito.importeAplicado, selectedVia.monedaCodigo)}</td>
                          <td>{hito.estado}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="empty-state">No hay hitos comerciales registrados.</p>
              )}
            </SectionCard>
          )}
        </>
      )}

      <AjusteCuotaModal
        open={ajustarModalOpen}
        cuota={selectedCuota}
        onClose={() => setAjustarModalOpen(false)}
        onSave={handleAjustarCuota}
        loading={modalLoading}
        error={modalError}
      />
      <AgregarCuotaModal
        open={agregarModalOpen}
        planPago={selectedVia?.planPago}
        onClose={() => setAgregarModalOpen(false)}
        onSave={handleAgregarCuota}
        loading={modalLoading}
        error={modalError}
      />
    </div>
  );
};

export default AcuerdoDetallePage;

import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import externalDataService from '../../comercial/services/externalDataService';
import cobranzasService from '../services/cobranzasService';
import '../../ventas/ventas.css';

const today = new Date().toISOString().slice(0, 10);

const initialForm = {
  clienteExternoId: '',
  fecha: today,
  monedaCodigo: 'ARS',
  cotizacion: 1,
  importeTotal: '',
  observaciones: '',
};

const initialMedio = {
  medioPagoCobranzaId: '',
  importe: '',
  bancoCobranzaId: '',
  banco: '',
  numeroReferencia: '',
  fechaEmision: today,
  fechaValor: today,
  librador: '',
  cuitLibrador: '',
  observaciones: '',
};

const initialAnulacion = {
  motivo: '',
};

const money = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const roundMoney = (value) => Math.round(Number(value || 0) * 100) / 100;

const CobranzasPage = () => {
  const [clientes, setClientes] = useState([]);
  const [mediosDisponibles, setMediosDisponibles] = useState([]);
  const [bancosDisponibles, setBancosDisponibles] = useState([]);
  const [cobranzas, setCobranzas] = useState([]);
  const [facturasDisponibles, setFacturasDisponibles] = useState([]);
  const [selectedCobranza, setSelectedCobranza] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [medioForm, setMedioForm] = useState(initialMedio);
  const [anulacionForm, setAnulacionForm] = useState(initialAnulacion);
  const [showAnulacionModal, setShowAnulacionModal] = useState(false);
  const [aplicacionesDraft, setAplicacionesDraft] = useState({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadCobranzas = async () => {
    const response = await cobranzasService.getCobranzas({ pageSize: 50 });
    setCobranzas(response.items || []);
  };

  const loadCobranzaDetalle = async (id) => {
    const [cobranza, aplicaciones] = await Promise.all([
      cobranzasService.getCobranza(id),
      cobranzasService.getAplicaciones(id),
    ]);
    return { ...cobranza, aplicacionesFactura: aplicaciones || [] };
  };

  useEffect(() => {
    let mounted = true;
    Promise.all([
      externalDataService.getClients(),
      cobranzasService.getMediosPago(true),
      cobranzasService.getBancos(true),
      cobranzasService.getCobranzas({ pageSize: 50 }),
    ])
      .then(([clientesData, mediosData, bancosData, cobranzasData]) => {
        if (!mounted) return;
        setClientes(clientesData || []);
        setMediosDisponibles(mediosData || []);
        setBancosDisponibles(bancosData || []);
        setCobranzas(cobranzasData.items || []);
      })
      .catch((loadError) => setError(getErrorMessage(loadError)))
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    if (!selectedCobranza?.id || selectedCobranza.estado !== 'Borrador') {
      return;
    }

    cobranzasService.getFacturasDisponibles(selectedCobranza.id)
      .then((data) => setFacturasDisponibles(data || []))
      .catch((loadError) => setError(getErrorMessage(loadError)));
  }, [selectedCobranza?.id, selectedCobranza?.estado]);

  const diferenciaMedios = useMemo(() => roundMoney(Number(selectedCobranza?.importeTotal || 0) - Number(selectedCobranza?.totalMedios || 0)), [selectedCobranza]);
  const diferenciaAplicaciones = useMemo(() => roundMoney(Number(selectedCobranza?.importeTotal || 0) - Number(selectedCobranza?.totalAplicado || 0)), [selectedCobranza]);

  const handleFormChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleMedioChange = (event) => {
    const { name, value } = event.target;
    setMedioForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreate = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const payload = {
        ...form,
        cotizacion: Number(form.cotizacion || 1),
        importeTotal: Number(form.importeTotal || 0),
      };
      const created = await cobranzasService.createCobranza(payload);
      setSelectedCobranza(created);
      setForm(initialForm);
      await loadCobranzas();
      setSuccess('Cobranza en borrador creada.');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleSelect = async (id) => {
    setError('');
    setSuccess('');
    const data = await loadCobranzaDetalle(id);
    setSelectedCobranza(data);
  };

  const handleAddMedio = async (event) => {
    event.preventDefault();
    if (!selectedCobranza) return;
    setSaving(true);
    setError('');
    try {
      const updated = await cobranzasService.addMedio(selectedCobranza.id, {
        ...medioForm,
        medioPagoCobranzaId: Number(medioForm.medioPagoCobranzaId),
        bancoCobranzaId: medioForm.bancoCobranzaId ? Number(medioForm.bancoCobranzaId) : null,
        importe: Number(medioForm.importe || 0),
      });
      setSelectedCobranza(updated);
      setMedioForm(initialMedio);
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleAddAplicacion = async (factura) => {
    if (!selectedCobranza) return;
    const importe = Number(aplicacionesDraft[factura.ventaId] || 0);
    setSaving(true);
    setError('');
    try {
      const updated = await cobranzasService.addAplicacion(selectedCobranza.id, {
        ventaId: factura.ventaId,
        importeAplicado: importe,
      });
      setSelectedCobranza(await loadCobranzaDetalle(updated.id));
      setAplicacionesDraft((prev) => ({ ...prev, [factura.ventaId]: '' }));
      setFacturasDisponibles(await cobranzasService.getFacturasDisponibles(selectedCobranza.id));
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteMedio = async (medioId) => {
    if (!selectedCobranza) return;
    const updated = await cobranzasService.deleteMedio(selectedCobranza.id, medioId);
    setSelectedCobranza(updated);
  };

  const handleDeleteAplicacion = async (aplicacionId) => {
    if (!selectedCobranza) return;
    const updated = await cobranzasService.deleteAplicacion(selectedCobranza.id, aplicacionId);
    setSelectedCobranza(await loadCobranzaDetalle(updated.id));
    setFacturasDisponibles(await cobranzasService.getFacturasDisponibles(selectedCobranza.id));
  };

  const handleApplyFullBalance = (factura) => {
    const pendienteCobranza = Math.max(roundMoney(Number(selectedCobranza?.importeTotal || 0) - Number(selectedCobranza?.totalAplicado || 0)), 0);
    const importe = Math.min(Number(factura.saldoDisponible || 0), pendienteCobranza || Number(factura.saldoDisponible || 0));
    setAplicacionesDraft((prev) => ({ ...prev, [factura.ventaId]: roundMoney(importe).toString() }));
  };

  const handleAutoPropose = () => {
    if (!selectedCobranza) return;
    let restante = Math.max(roundMoney(Number(selectedCobranza.importeTotal || 0) - Number(selectedCobranza.totalAplicado || 0)), 0);
    const nextDraft = { ...aplicacionesDraft };
    facturasDisponibles.forEach((factura) => {
      if (restante <= 0) return;
      const importe = Math.min(Number(factura.saldoDisponible || 0), restante);
      if (importe > 0) {
        nextDraft[factura.ventaId] = roundMoney(importe).toString();
        restante = roundMoney(restante - importe);
      }
    });
    setAplicacionesDraft(nextDraft);
  };

  const handleConfirmar = async () => {
    if (!selectedCobranza) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const response = await cobranzasService.confirmar(selectedCobranza.id);
      setSelectedCobranza(response.cobranza);
      await loadCobranzas();
      setSuccess(`Cobranza confirmada. Asiento ${response.asientoContableId}.`);
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const openAnulacionModal = () => {
    setAnulacionForm(initialAnulacion);
    setError('');
    setSuccess('');
    setShowAnulacionModal(true);
  };

  const closeAnulacionModal = () => {
    if (saving) return;
    setShowAnulacionModal(false);
    setAnulacionForm(initialAnulacion);
  };

  const handleAnular = async (event) => {
    event.preventDefault();
    if (!selectedCobranza) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const response = await cobranzasService.anular(selectedCobranza.id, { motivo: anulacionForm.motivo });
      setSelectedCobranza(response.cobranza);
      setShowAnulacionModal(false);
      setAnulacionForm(initialAnulacion);
      await loadCobranzas();
      setSuccess(`Cobranza anulada. Asiento de reversion ${response.asientoReversionId}.`);
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const selectedMedio = mediosDisponibles.find((m) => String(m.id) === String(medioForm.medioPagoCobranzaId));
  const selectedMedioIsCheque = selectedMedio?.codigo === 'CHEQUE';
  const canEdit = selectedCobranza?.estado === 'Borrador';
  const canAnular = selectedCobranza?.estado === 'Confirmada';
  const confirmDisabledReason = useMemo(() => {
    if (!selectedCobranza || !canEdit) return '';
    if (selectedCobranza.mediosPago.length === 0) return 'Debe cargar al menos un medio de pago.';
    if (selectedCobranza.aplicacionesFactura.length === 0) return 'Debe aplicar la cobranza a al menos una factura.';
    if (diferenciaMedios !== 0) return 'La suma de medios debe coincidir con el importe total.';
    if (diferenciaAplicaciones !== 0) return 'La suma aplicada a facturas debe coincidir con el importe total.';
    return '';
  }, [canEdit, diferenciaAplicaciones, diferenciaMedios, selectedCobranza]);

  return (
    <div className="page-container ventas-page cobranzas-page">
      <div className="page-header">
        <div>
          <h1>Cobranzas de Via 1</h1>
          <p className="page-subtitle">Registro de cancelaciones efectivas sobre facturas confirmadas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/cartera-cheques">Cartera de cheques</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
          <Link className="btn-secondary" to="/">Principal</Link>
        </div>
      </div>

      <SectionCard title="Nueva cobranza">
        <form className="venta-form" onSubmit={handleCreate}>
          <div className="form-grid">
            <div className="form-field">
              <label>Cliente</label>
              <select name="clienteExternoId" value={form.clienteExternoId} onChange={handleFormChange} required>
                <option value="">Selecciona un cliente</option>
                {clientes.map((cliente) => (
                  <option key={cliente.idCliente} value={String(cliente.idCliente)}>{cliente.nombreCliente}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Fecha</label>
              <input name="fecha" type="date" value={form.fecha} onChange={handleFormChange} required />
            </div>
            <div className="form-field">
              <label>Moneda</label>
              <select name="monedaCodigo" value={form.monedaCodigo} onChange={handleFormChange} required>
                <option value="ARS">ARS</option>
                <option value="USD">USD</option>
              </select>
            </div>
            <div className="form-field">
              <label>Cotizacion</label>
              <input name="cotizacion" type="number" min="0.000001" step="0.000001" value={form.cotizacion} onChange={handleFormChange} required />
            </div>
            <div className="form-field">
              <label>Importe total</label>
              <input name="importeTotal" type="number" min="0.01" step="0.01" value={form.importeTotal} onChange={handleFormChange} required />
            </div>
            <div className="form-field">
              <label>Observaciones</label>
              <input name="observaciones" value={form.observaciones} onChange={handleFormChange} />
            </div>
          </div>
          <div className="form-actions">
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar borrador'}</button>
          </div>
        </form>
      </SectionCard>

      <SectionCard title="Cobranzas registradas">
        {loading ? <LoadingSpinner /> : (
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Cliente</th>
                  <th>Importe</th>
                  <th>Moneda</th>
                  <th>Estado</th>
                  <th>Facturas</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {cobranzas.map((cobranza) => (
                  <tr key={cobranza.id}>
                    <td>{String(cobranza.fecha).slice(0, 10)}</td>
                    <td>{cobranza.clienteNombre || cobranza.clienteExternoId}</td>
                    <td>{money(cobranza.importeTotal)}</td>
                    <td>{cobranza.monedaCodigo}</td>
                    <td><span className={`status-pill ${cobranza.estado === 'Borrador' ? 'is-draft' : cobranza.estado === 'Anulada' ? 'is-inactive' : 'is-active'}`}>{cobranza.estado}</span></td>
                    <td>{cobranza.cantidadFacturasAplicadas}</td>
                    <td><button className="btn-secondary" type="button" onClick={() => handleSelect(cobranza.id)}>Abrir</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      {selectedCobranza && (
        <SectionCard
          title={`Cobranza ${selectedCobranza.id}`}
          actions={(
            <>
              {canEdit && (
                <button className="btn-primary" type="button" onClick={handleConfirmar} disabled={saving || Boolean(confirmDisabledReason)} title={confirmDisabledReason}>
                  Confirmar
                </button>
              )}
              {canAnular && (
                <button className="btn-secondary" type="button" onClick={openAnulacionModal} disabled={saving}>
                  Anular
                </button>
              )}
            </>
          )}
        >
          <div className="summary-grid">
            <div><span>Cliente</span><strong>{selectedCobranza.clienteNombre || selectedCobranza.clienteExternoId}</strong></div>
            <div><span>Total</span><strong>{selectedCobranza.monedaCodigo} {money(selectedCobranza.importeTotal)}</strong></div>
            <div><span>Medios</span><strong>{money(selectedCobranza.totalMedios)}</strong></div>
            <div><span>Aplicado</span><strong>{money(selectedCobranza.totalAplicado)}</strong></div>
            <div><span>Diferencia medios</span><strong>{money(diferenciaMedios)}</strong></div>
            <div><span>Diferencia aplicacion</span><strong>{money(diferenciaAplicaciones)}</strong></div>
            {selectedCobranza.fechaAnulacion && <div><span>Fecha anulacion</span><strong>{String(selectedCobranza.fechaAnulacion).slice(0, 10)}</strong></div>}
            {selectedCobranza.usuarioAnulacion && <div><span>Usuario anulacion</span><strong>{selectedCobranza.usuarioAnulacion}</strong></div>}
          </div>
          {confirmDisabledReason && <p className="form-warning">{confirmDisabledReason}</p>}
          {selectedCobranza.motivoAnulacion && <p className="form-warning">{selectedCobranza.motivoAnulacion}</p>}

          {canEdit && (
            <form className="venta-form" onSubmit={handleAddMedio}>
              <div className="form-grid">
                <div className="form-field">
                  <label>Medio</label>
                  <select name="medioPagoCobranzaId" value={medioForm.medioPagoCobranzaId} onChange={handleMedioChange} required>
                    <option value="">Selecciona un medio</option>
                    {mediosDisponibles.map((medio) => (
                      <option key={medio.id} value={medio.id}>{medio.descripcion}</option>
                    ))}
                  </select>
                </div>
                <div className="form-field">
                  <label>Importe</label>
                  <input name="importe" type="number" min="0.01" step="0.01" value={medioForm.importe} onChange={handleMedioChange} required />
                </div>
                {selectedMedio?.requiereBanco && (
                  <div className="form-field">
                    <label>Banco</label>
                    <select name="bancoCobranzaId" value={medioForm.bancoCobranzaId} onChange={handleMedioChange} required>
                      <option value="">Selecciona un banco</option>
                      {bancosDisponibles.map((banco) => (
                        <option key={banco.id} value={banco.id}>{banco.nombre}</option>
                      ))}
                    </select>
                  </div>
                )}
                {selectedMedio?.requiereReferencia && (
                  <div className="form-field">
                    <label>{selectedMedioIsCheque ? 'Numero de cheque' : 'Referencia'}</label>
                    <input name="numeroReferencia" value={medioForm.numeroReferencia} onChange={handleMedioChange} required />
                  </div>
                )}
                {selectedMedioIsCheque && (
                  <div className="form-field">
                    <label>Fecha de emision</label>
                    <input name="fechaEmision" type="date" value={medioForm.fechaEmision} onChange={handleMedioChange} required />
                  </div>
                )}
                {selectedMedio?.requiereFechaValor && (
                  <div className="form-field">
                    <label>{selectedMedioIsCheque ? 'Fecha de vencimiento' : 'Fecha valor'}</label>
                    <input name="fechaValor" type="date" value={medioForm.fechaValor} onChange={handleMedioChange} required />
                  </div>
                )}
                {selectedMedioIsCheque && (
                  <>
                    <div className="form-field">
                      <label>Librador</label>
                      <input name="librador" value={medioForm.librador} onChange={handleMedioChange} required />
                    </div>
                    <div className="form-field">
                      <label>CUIT librador</label>
                      <input name="cuitLibrador" value={medioForm.cuitLibrador} onChange={handleMedioChange} required />
                    </div>
                  </>
                )}
                <div className="form-field">
                  <label>Observaciones</label>
                  <input name="observaciones" value={medioForm.observaciones} onChange={handleMedioChange} />
                </div>
              </div>
              <div className="form-actions">
                <button className="btn-secondary" type="submit" disabled={saving}>Agregar medio</button>
              </div>
            </form>
          )}

          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Medio</th>
                  <th>Importe</th>
                  <th>Referencia</th>
                  <th>Detalle cheque</th>
                  <th>Concepto</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {selectedCobranza.mediosPago.map((medio) => (
                  <tr key={medio.id}>
                    <td>{medio.medioPagoDescripcion}</td>
                    <td>{money(medio.importe)}</td>
                    <td>{[medio.banco, medio.numeroReferencia].filter(Boolean).join(' / ') || '-'}</td>
                    <td>{medio.medioPagoCodigo === 'CHEQUE' ? [medio.librador, medio.cuitLibrador, medio.fechaValor && String(medio.fechaValor).slice(0, 10)].filter(Boolean).join(' / ') : '-'}</td>
                    <td>{medio.codigoConceptoContable}</td>
                    <td>{canEdit && <button className="btn-secondary" type="button" onClick={() => handleDeleteMedio(medio.id)}>Quitar</button>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </SectionCard>
      )}

      {selectedCobranza && canEdit && (
        <SectionCard
          title="Facturas pendientes"
          actions={<button className="btn-secondary" type="button" onClick={handleAutoPropose} disabled={saving || facturasDisponibles.length === 0}>Aplicar automaticamente</button>}
        >
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Comprobante</th>
                  <th>Fecha</th>
                  <th>Obra</th>
                  <th>Moneda</th>
                  <th>Total</th>
                  <th>Cobrado</th>
                  <th>Reservado</th>
                  <th>Disponible</th>
                  <th>Aplicar</th>
                </tr>
              </thead>
              <tbody>
                {facturasDisponibles.length === 0 && (
                  <tr>
                    <td colSpan="9" className="empty-cell">No hay facturas confirmadas con saldo pendiente para este cliente y moneda.</td>
                  </tr>
                )}
                {facturasDisponibles.map((factura) => (
                  <tr key={factura.ventaId}>
                    <td>{factura.comprobante}</td>
                    <td>{String(factura.fechaComprobante).slice(0, 10)}</td>
                    <td>{factura.obraNombre || factura.obraExternaId}</td>
                    <td>{factura.monedaCodigo}</td>
                    <td>{money(factura.total)}</td>
                    <td>{money(factura.cobradoConfirmado)}</td>
                    <td>{money(factura.reservadoBorrador)}</td>
                    <td>{money(factura.saldoDisponible)}</td>
                    <td>
                      <div className="inline-entry">
                        <input
                          type="number"
                          min="0.01"
                          max={factura.saldoDisponible}
                          step="0.01"
                          value={aplicacionesDraft[factura.ventaId] || ''}
                          onChange={(event) => setAplicacionesDraft((prev) => ({ ...prev, [factura.ventaId]: event.target.value }))}
                        />
                        <button className="btn-secondary" type="button" onClick={() => handleApplyFullBalance(factura)} disabled={saving}>Saldo</button>
                        <button className="btn-secondary" type="button" onClick={() => handleAddAplicacion(factura)} disabled={saving}>Aplicar</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </SectionCard>
      )}

      {selectedCobranza && (
        <SectionCard title="Aplicaciones a facturas">
          <div className="summary-grid">
            <div><span>Importe cobranza</span><strong>{money(selectedCobranza.importeTotal)}</strong></div>
            <div><span>Total aplicado</span><strong>{money(selectedCobranza.totalAplicado)}</strong></div>
            <div><span>Pendiente de aplicar</span><strong>{money(diferenciaAplicaciones)}</strong></div>
          </div>
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Factura</th>
                  <th>Importe</th>
                  <th>Distribucion</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {selectedCobranza.aplicacionesFactura.map((aplicacion) => (
                  <tr key={aplicacion.id}>
                    <td>{aplicacion.comprobante}</td>
                    <td>{money(aplicacion.importeAplicado)}</td>
                    <td>
                      {aplicacion.aplicacionesObligacion.length === 0 && (
                        <span className="table-subtext">Pendiente para confirmacion</span>
                      )}
                      {aplicacion.aplicacionesObligacion.map((obligacion) => (
                        <span className="table-subtext" key={obligacion.id || `${obligacion.cuotaComercialId}-${obligacion.importeAplicado}`}>
                          {obligacion.tipoObligacion} {obligacion.numeroCuota}: {money(obligacion.importeAplicado)}
                        </span>
                      ))}
                    </td>
                    <td>{canEdit && <button className="btn-secondary" type="button" onClick={() => handleDeleteAplicacion(aplicacion.id)}>Quitar</button>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </SectionCard>
      )}

      {error && <p className="form-error">{error}</p>}
      {success && <p className="info-banner">{success}</p>}

      {showAnulacionModal && (
        <div className="modal-backdrop">
          <form className="modal-card" onSubmit={handleAnular}>
            <div className="modal-header">
              <h2>Anular cobranza</h2>
              <button className="modal-close" type="button" onClick={closeAnulacionModal} aria-label="Cerrar">x</button>
            </div>
            <div className="modal-body">
              <p className="form-warning">La anulacion revertira los efectos de la cobranza en la cuenta corriente, el plan de pago y la contabilidad.</p>
              <div className="form-field">
                <label>Motivo</label>
                <textarea
                  value={anulacionForm.motivo}
                  onChange={(event) => setAnulacionForm({ motivo: event.target.value })}
                  required
                  rows="4"
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-secondary" type="button" onClick={closeAnulacionModal} disabled={saving}>Cancelar</button>
              <button className="btn-primary" type="submit" disabled={saving || !anulacionForm.motivo.trim()}>
                {saving ? 'Anulando...' : 'Confirmar anulacion'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};

export default CobranzasPage;

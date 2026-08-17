import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import VentaDetalleForm from '../components/VentaDetalleForm';
import VentaDetalleTable from '../components/VentaDetalleTable';
import VentaPercepcionIibbPanel from '../components/VentaPercepcionIibbPanel';
import VentaTotalsPanel from '../components/VentaTotalsPanel';
import ventasService from '../services/ventasService';
import '../ventas.css';

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const formatDate = (value) => (value ? new Date(value).toLocaleDateString('es-AR') : '-');
const formatNumber = (value, size) => String(value || 0).padStart(size, '0');
const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const VentaDetallePage = () => {
  const { ventaId } = useParams();
  const [venta, setVenta] = useState(null);
  const [alicuotas, setAlicuotas] = useState([]);
  const [nomencladores, setNomencladores] = useState([]);
  const [itemsFacturables, setItemsFacturables] = useState([]);
  const [regimenesIibb, setRegimenesIibb] = useState([]);
  const [clientePercepcionConfig, setClientePercepcionConfig] = useState(null);
  const [percepcionIibb, setPercepcionIibb] = useState(null);
  const [selectedDetalle, setSelectedDetalle] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [calculatingPercepcion, setCalculatingPercepcion] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [confirmacionValidacion, setConfirmacionValidacion] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const isConfirmed = venta?.estado === 'Confirmada';

  const loadData = async () => {
    setLoading(true);
    setError('');
    try {
      const ventaData = await ventasService.getVenta(ventaId);
      const [alicuotasData, nomencladoresData, itemsData, regimenesData, configData, percepcionData] = await Promise.all([
        ventasService.getAlicuotasIva({ soloActivos: true }),
        ventasService.getNomencladores({ soloActivos: true }),
        ventasService.getItemsFacturables({ soloActivos: true }),
        ventasService.getPercepcionesIibb({ soloActivos: true, soloVigentes: true }),
        ventasService.getClientePercepcionIibbConfig(ventaData.clienteExternoId),
        ventasService.getVentaPercepcionIibb(ventaId),
      ]);
      setVenta(ventaData);
      setAlicuotas(alicuotasData || []);
      setNomencladores(nomencladoresData || []);
      setItemsFacturables(itemsData || []);
      setRegimenesIibb(regimenesData || []);
      setClientePercepcionConfig(configData);
      setPercepcionIibb(percepcionData);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    ventasService.getVenta(ventaId)
      .then((ventaData) => Promise.all([
        Promise.resolve(ventaData),
        ventasService.getAlicuotasIva({ soloActivos: true }),
        ventasService.getNomencladores({ soloActivos: true }),
        ventasService.getItemsFacturables({ soloActivos: true }),
        ventasService.getPercepcionesIibb({ soloActivos: true, soloVigentes: true }),
        ventasService.getClientePercepcionIibbConfig(ventaData.clienteExternoId),
        ventasService.getVentaPercepcionIibb(ventaId),
      ]))
      .then(([ventaData, alicuotasData, nomencladoresData, itemsData, regimenesData, configData, percepcionData]) => {
        if (!mounted) return;
        setVenta(ventaData);
        setAlicuotas(alicuotasData || []);
        setNomencladores(nomencladoresData || []);
        setItemsFacturables(itemsData || []);
        setRegimenesIibb(regimenesData || []);
        setClientePercepcionConfig(configData);
        setPercepcionIibb(percepcionData);
      })
      .catch((loadError) => {
        if (mounted) setError(getErrorMessage(loadError));
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, [ventaId]);

  const handleSubmitDetalle = async (payload) => {
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const response = selectedDetalle?.id
        ? await ventasService.updateVentaDetalle(ventaId, selectedDetalle.id, payload)
        : await ventasService.createVentaDetalle(ventaId, payload);

      setVenta(response.venta);
      setPercepcionIibb(response.venta?.percepcionesIibb?.[0] || percepcionIibb);
      setSelectedDetalle(null);
      setMessage(selectedDetalle?.id ? 'Detalle actualizado.' : 'Detalle agregado.');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteDetalle = async (detalle) => {
    if (!window.confirm(`Eliminar la linea ${detalle.numeroLinea}?`)) return;
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const updatedVenta = await ventasService.deleteVentaDetalle(ventaId, detalle.id);
      setVenta(updatedVenta);
      setPercepcionIibb(updatedVenta?.percepcionesIibb?.[0] || percepcionIibb);
      setSelectedDetalle(null);
      setMessage('Detalle eliminado.');
    } catch (deleteError) {
      setError(getErrorMessage(deleteError));
    } finally {
      setSaving(false);
    }
  };

  const handleSavePercepcionConfig = async (payload) => {
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const config = await ventasService.saveClientePercepcionIibbConfig(venta.clienteExternoId, payload);
      setClientePercepcionConfig(config);
      setMessage('Configuracion tributaria del cliente guardada.');
    } catch (configError) {
      setError(getErrorMessage(configError));
    } finally {
      setSaving(false);
    }
  };

  const handleCalcularPercepcion = async () => {
    setCalculatingPercepcion(true);
    setMessage('');
    setError('');
    try {
      const result = await ventasService.calcularVentaPercepcionIibb(ventaId);
      setPercepcionIibb(result.percepcion);
      setClientePercepcionConfig(result.configuracionCliente);
      if (result.venta) setVenta(result.venta);
      setMessage('Percepcion de IIBB Entre Rios calculada.');
    } catch (calcError) {
      setError(getErrorMessage(calcError));
    } finally {
      setCalculatingPercepcion(false);
    }
  };

  const handleValidarConfirmacion = async () => {
    setConfirming(true);
    setMessage('');
    setError('');
    try {
      const validacion = await ventasService.validarConfirmacionFactura(ventaId);
      setConfirmacionValidacion(validacion);
      if (!validacion.esValida) {
        setError(validacion.errores?.[0] || 'La factura no puede confirmarse.');
        return null;
      }
      return validacion;
    } catch (validationError) {
      setError(getErrorMessage(validationError));
      return null;
    } finally {
      setConfirming(false);
    }
  };

  const handleConfirmarFactura = async () => {
    const validacion = await handleValidarConfirmacion();
    if (!validacion?.esValida) return;

    const resumen = [
      `Comprobante: ${venta.tipoComprobanteDescripcion}`,
      `Numero: ${formatNumber(venta.puntoVenta, 4)}-${formatNumber(venta.numeroComprobante, 8)}`,
      `Cliente: ${venta.clienteNombre || venta.clienteExternoId}`,
      `Obra: ${venta.obraNombre || venta.obraExternaId}`,
      `Total final: ${formatMoney(venta.total)}`,
      '',
      'Al confirmar, la factura quedara bloqueada y generara la deuda contable del cliente.',
      'Esta accion no registra un pago.',
    ].join('\n');

    if (!window.confirm(resumen)) return;

    setConfirming(true);
    setMessage('');
    setError('');
    try {
      const result = await ventasService.confirmarFactura(ventaId);
      setVenta(result.venta);
      setPercepcionIibb(result.venta?.percepcionesIibb?.[0] || percepcionIibb);
      setSelectedDetalle(null);
      setConfirmacionValidacion(null);
      setMessage(`Factura confirmada. Asiento contable: ${result.asientoContableId}.`);
    } catch (confirmError) {
      setError(getErrorMessage(confirmError));
    } finally {
      setConfirming(false);
    }
  };

  if (loading) {
    return (
      <div className="page-container ventas-page">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="page-container ventas-page">
      <div className="page-header">
        <div>
          <h1>Factura Borrador</h1>
          <p className="page-subtitle">Carga de detalles y totales calculados en backend.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
          <button className="btn-secondary" type="button" onClick={loadData} disabled={loading || saving}>Actualizar</button>
          {venta && !isConfirmed && (
            <button className="btn-primary" type="button" onClick={handleConfirmarFactura} disabled={saving || confirming || calculatingPercepcion}>
              {confirming ? 'Confirmando...' : 'Confirmar factura'}
            </button>
          )}
        </div>
      </div>

      {error && <p className="form-error">{error}</p>}
      {message && <p className="form-success">{message}</p>}

      {venta && (
        <>
          <SectionCard title="Encabezado">
            <div className="summary-grid">
              <div>
                <span>Comprobante</span>
                <strong>{venta.tipoComprobanteDescripcion}</strong>
              </div>
              <div>
                <span>Numero</span>
                <strong>{formatNumber(venta.puntoVenta, 4)}-{formatNumber(venta.numeroComprobante, 8)}</strong>
              </div>
              <div>
                <span>Fecha</span>
                <strong>{formatDate(venta.fechaComprobante)}</strong>
              </div>
              <div>
                <span>Cliente</span>
                <strong>{venta.clienteNombre || venta.clienteExternoId}</strong>
              </div>
              <div>
                <span>Obra</span>
                <strong>{venta.obraNombre || venta.obraExternaId}</strong>
              </div>
              <div>
                <span>Moneda</span>
                <strong>{venta.monedaCodigo}</strong>
              </div>
              <div>
                <span>Estado</span>
                <strong>{venta.estado}</strong>
              </div>
              {venta.asientoContableId && (
                <div>
                  <span>Asiento contable</span>
                  <strong>{venta.asientoContableId}</strong>
                </div>
              )}
              {venta.fechaConfirmacion && (
                <div>
                  <span>Confirmacion</span>
                  <strong>{formatDate(venta.fechaConfirmacion)}</strong>
                </div>
              )}
            </div>
            <div className="tag-row">
              {venta.tipoComprobanteEsExportacion && <span className="tag">Exportacion</span>}
              {venta.tipoComprobanteEsCreditoElectronica && <span className="tag">FCE</span>}
              {venta.tipoComprobanteRequiereNomenclador && <span className="tag">Requiere nomenclador</span>}
              {venta.tipoComprobantePermiteIva && <span className="tag">Permite IVA</span>}
            </div>
          </SectionCard>

          {!isConfirmed && (
            <SectionCard title={selectedDetalle ? 'Editar detalle' : 'Nuevo detalle'}>
              <VentaDetalleForm
                venta={venta}
                itemsFacturables={itemsFacturables}
                alicuotas={alicuotas}
                nomencladores={nomencladores}
                selectedDetalle={selectedDetalle}
                saving={saving}
                onCancel={() => setSelectedDetalle(null)}
                onSubmit={handleSubmitDetalle}
              />
            </SectionCard>
          )}

          {!isConfirmed && (
            <SectionCard title="Confirmacion definitiva">
              <div className="summary-grid">
                <div><span>Comprobante</span><strong>{venta.tipoComprobanteDescripcion}</strong></div>
                <div><span>Punto de venta</span><strong>{formatNumber(venta.puntoVenta, 4)}</strong></div>
                <div><span>Numero</span><strong>{formatNumber(venta.numeroComprobante, 8)}</strong></div>
                <div><span>Neto</span><strong>{formatMoney((venta.totalAntesPercepciones || 0) - (venta.totalIva || 0))}</strong></div>
                <div><span>IVA</span><strong>{formatMoney(venta.totalIva)}</strong></div>
                <div><span>Percepcion</span><strong>{formatMoney(venta.totalPercepciones)}</strong></div>
                <div><span>Total final</span><strong>{formatMoney(venta.total)}</strong></div>
                <div><span>Obligaciones aplicadas</span><strong>{confirmacionValidacion?.cantidadObligacionesAplicadas ?? 0}</strong></div>
              </div>
              <p className="form-warning">Al confirmar, la factura quedara bloqueada y generara la deuda contable del cliente. Esta accion no registra un pago.</p>
              {confirmacionValidacion?.errores?.length > 0 && (
                <ul className="form-error">
                  {confirmacionValidacion.errores.map((item) => <li key={item}>{item}</li>)}
                </ul>
              )}
            </SectionCard>
          )}

          <SectionCard title="Detalles">
            <VentaDetalleTable
              detalles={venta.detalles || []}
              saving={saving || confirming}
              readOnly={isConfirmed}
              onEdit={setSelectedDetalle}
              onDelete={handleDeleteDetalle}
            />
          </SectionCard>

          <SectionCard title="Tributos adicionales">
            <VentaPercepcionIibbPanel
              venta={venta}
              percepcion={percepcionIibb}
              clienteConfig={clientePercepcionConfig}
              regimenes={regimenesIibb}
              saving={saving || confirming}
              calculating={calculatingPercepcion}
              readOnly={isConfirmed}
              onSaveConfig={handleSavePercepcionConfig}
              onCalcular={handleCalcularPercepcion}
            />
          </SectionCard>

          <SectionCard title="Totales">
            <VentaTotalsPanel venta={venta} />
          </SectionCard>
        </>
      )}
    </div>
  );
};

export default VentaDetallePage;

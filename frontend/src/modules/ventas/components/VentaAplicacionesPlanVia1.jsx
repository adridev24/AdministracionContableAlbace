import { useEffect, useMemo, useState } from 'react';
import SectionCard from '../../../shared/components/SectionCard';
import ventasService from '../services/ventasService';

const formatDate = (value) => (value ? new Date(value).toLocaleDateString('es-AR') : '-');
const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudieron guardar las aplicaciones al plan.';

const getImporteAsociable = (venta) => Number((venta?.totalAntesPercepciones || 0) - (venta?.totalIva || 0));

const VentaAplicacionesPlanVia1 = ({ venta, readOnly = false, disabled = false, onSaved }) => {
  const [obligaciones, setObligaciones] = useState([]);
  const [vinculaciones, setVinculaciones] = useState([]);
  const [aplicaciones, setAplicaciones] = useState({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const ventaId = venta?.id;
  const importeAsociable = getImporteAsociable(venta);

  const loadData = async () => {
    if (!ventaId) return;
    setLoading(true);
    setError('');
    try {
      const [obligacionesData, vinculacionesData] = await Promise.all([
        ventasService.getObligacionesVia1Disponibles(ventaId),
        ventasService.getVinculacionesPlan(ventaId),
      ]);
      const nextObligaciones = obligacionesData || [];
      const nextVinculaciones = vinculacionesData || [];
      const nextAplicaciones = {};
      nextObligaciones.forEach((item) => {
        nextAplicaciones[item.obligacionId] = item.importeAplicadoFacturaActual || 0;
      });
      nextVinculaciones.forEach((item) => {
        nextAplicaciones[item.obligacionId] = item.importeAplicado || 0;
      });
      setObligaciones(nextObligaciones);
      setVinculaciones(nextVinculaciones);
      setAplicaciones(nextAplicaciones);
    } catch (loadError) {
      setError(loadError?.response?.data?.error || 'No se pudieron cargar las obligaciones de Via 1.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [ventaId]);

  const rows = useMemo(() => {
    const byId = new Map();
    obligaciones.forEach((item) => byId.set(item.obligacionId, item));
    vinculaciones.forEach((item) => {
      if (!byId.has(item.obligacionId)) {
        byId.set(item.obligacionId, {
          obligacionId: item.obligacionId,
          tipoObligacion: item.tipo,
          numeroCuota: item.numeroCuota,
          fechaVencimiento: item.fechaVencimiento,
          numeroAcuerdo: item.numeroAcuerdo,
          monedaCodigo: venta?.monedaCodigo || '',
          importePrevisto: item.importeAplicado,
          importeFacturadoConfirmado: 0,
          importeReservado: 0,
          saldoDisponible: item.importeAplicado,
          importeAplicadoFacturaActual: item.importeAplicado,
        });
      }
    });
    return Array.from(byId.values());
  }, [obligaciones, vinculaciones, venta?.monedaCodigo]);

  const totalAplicado = useMemo(
    () => Object.values(aplicaciones).reduce((sum, value) => sum + Number(value || 0), 0),
    [aplicaciones],
  );
  const pendienteAplicar = importeAsociable - totalAplicado;

  if (!loading && rows.length === 0) {
    return null;
  }

  const setImporte = (obligacionId, value) => {
    setAplicaciones((prev) => ({ ...prev, [obligacionId]: value }));
  };

  const aplicarSaldo = (row) => {
    const saldo = Number(row.saldoDisponible || 0);
    const pendiente = Math.max(importeAsociable - totalAplicado + Number(aplicaciones[row.obligacionId] || 0), 0);
    setImporte(row.obligacionId, Math.min(saldo, pendiente).toFixed(2));
  };

  const quitarAplicacion = (row) => {
    setImporte(row.obligacionId, '');
  };

  const handleSave = async () => {
    const payload = Object.entries(aplicaciones)
      .map(([obligacionId, importeAplicado]) => ({
        obligacionId: Number(obligacionId),
        importeAplicado: Number(importeAplicado || 0),
      }))
      .filter((item) => item.importeAplicado > 0);

    setSaving(true);
    setError('');
    setMessage('');
    try {
      await ventasService.updateVinculacionesPlan(ventaId, payload);
      await loadData();
      setMessage('Aplicaciones al plan guardadas.');
      if (onSaved) await onSaved();
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <SectionCard title="Aplicacion al Plan de Pago" description="Asociacion comercial de la factura con obligaciones de Via 1.">
      {loading ? (
        <p className="empty-state">Cargando obligaciones de Via 1...</p>
      ) : (
        <>
          <div className="summary-grid">
            <div><span>Importe asociable</span><strong>{formatMoney(importeAsociable)}</strong></div>
            <div><span>Total aplicado</span><strong>{formatMoney(totalAplicado)}</strong></div>
            <div><span>Pendiente de aplicar</span><strong>{formatMoney(pendienteAplicar)}</strong></div>
          </div>

          {error && <p className="form-error">{error}</p>}
          {message && <p className="form-success">{message}</p>}

          <div className="table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Obligacion</th>
                  <th>Vencimiento</th>
                  <th>Importe previsto</th>
                  <th>Facturado</th>
                  <th>Reservado</th>
                  <th>Saldo disponible</th>
                  <th>Importe a aplicar</th>
                  {!readOnly && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.obligacionId}>
                    <td>
                      <strong>{row.tipoObligacion === 'Anticipo' ? 'Anticipo' : `Cuota ${row.numeroCuota}`}</strong>
                      <span className="table-subtext">{row.numeroAcuerdo}</span>
                    </td>
                    <td>{formatDate(row.fechaVencimiento)}</td>
                    <td>{formatMoney(row.importePrevisto)}</td>
                    <td>{formatMoney(row.importeFacturadoConfirmado)}</td>
                    <td>{formatMoney(row.importeReservado)}</td>
                    <td>{formatMoney(row.saldoDisponible)}</td>
                    <td>
                      {readOnly ? (
                        <strong>{formatMoney(aplicaciones[row.obligacionId])}</strong>
                      ) : (
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          value={aplicaciones[row.obligacionId] ?? ''}
                          onChange={(event) => setImporte(row.obligacionId, event.target.value)}
                          disabled={disabled || saving}
                        />
                      )}
                    </td>
                    {!readOnly && (
                      <td>
                        <div className="row-actions">
                          <button className="btn-secondary" type="button" onClick={() => aplicarSaldo(row)} disabled={disabled || saving}>
                            Aplicar saldo
                          </button>
                          <button className="btn-secondary" type="button" onClick={() => quitarAplicacion(row)} disabled={disabled || saving}>
                            Quitar
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {!readOnly && (
            <div className="form-actions">
              <button className="btn-primary" type="button" onClick={handleSave} disabled={disabled || saving}>
                {saving ? 'Guardando...' : 'Guardar aplicaciones'}
              </button>
            </div>
          )}
        </>
      )}
    </SectionCard>
  );
};

export default VentaAplicacionesPlanVia1;

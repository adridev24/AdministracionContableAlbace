import { useEffect, useState } from 'react';
import SituacionVia1DetalleModal from './SituacionVia1DetalleModal';
import acuerdosService from '../services/acuerdosService';

const currency = (value, moneda = 'ARS') =>
  `${moneda ? `${moneda} ` : ''}${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const formatDate = (value) => {
  if (!value) return '-';
  const [year, month, day] = String(value).slice(0, 10).split('-');
  return year && month && day ? `${day}/${month}/${year}` : '-';
};

const estadoLabel = {
  SIN_FACTURAR: 'Sin facturar',
  PARCIALMENTE_FACTURADA: 'Parcial',
  FACTURADA: 'Facturada',
  SIN_COBRAR: 'Sin cobrar',
  PARCIALMENTE_COBRADA: 'Parcial',
  COBRADA: 'Cobrada',
};

const SituacionVia1Panel = ({ acuerdoId, enabled }) => {
  const [situacion, setSituacion] = useState(null);
  const [selectedObligacion, setSelectedObligacion] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!enabled || !acuerdoId) return;

    let cancelled = false;
    setLoading(true);
    setError('');

    acuerdosService.getSituacionVia1(acuerdoId)
      .then((data) => {
        if (!cancelled) setSituacion(data);
      })
      .catch(() => {
        if (!cancelled) setError('No se pudo cargar la situacion de Via 1.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [acuerdoId, enabled]);

  if (!enabled) return null;

  const moneda = situacion?.monedaCodigo || 'ARS';
  const resumen = situacion ? [
    { label: 'Acordado', value: situacion.montoAcordado },
    { label: 'Facturado', value: situacion.totalFacturado },
    { label: 'Pend. facturar', value: situacion.pendienteFacturar },
    { label: 'Cobrado', value: situacion.totalCobrado },
    { label: 'Pend. cobro', value: situacion.facturadoPendienteCobro },
    { label: 'Pendiente total', value: situacion.pendienteTotal },
  ] : [];

  return (
    <>
      <div className="situacion-via1">
        {loading && <p className="empty-state">Cargando situacion de Via 1...</p>}
        {error && <p className="form-error">{error}</p>}

        {!loading && !error && situacion && (
          <>
            <div className="summary-grid situacion-summary">
              {resumen.map((item) => (
                <div className="summary-card" key={item.label}>
                  <span>{item.label}</span>
                  <strong>{currency(item.value, moneda)}</strong>
                </div>
              ))}
            </div>

            {situacion.totalReservadoBorradores > 0 && (
              <div className="alert-box">
                Reservado en facturas borrador: {currency(situacion.totalReservadoBorradores, moneda)}
              </div>
            )}

            {situacion.obligaciones?.length ? (
              <div className="table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Obligacion</th>
                      <th>Vencimiento</th>
                      <th>Previsto</th>
                      <th>Facturado</th>
                      <th>Pend. facturar</th>
                      <th>Cobrado</th>
                      <th>Pend. cobro</th>
                      <th>Pend. total</th>
                      <th>Facturacion</th>
                      <th>Cobranza</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {situacion.obligaciones.map((obligacion) => (
                      <tr key={obligacion.obligacionId}>
                        <td>{obligacion.tipo} {obligacion.numero}</td>
                        <td>{formatDate(obligacion.fechaVencimiento)}</td>
                        <td>{currency(obligacion.importePrevisto, moneda)}</td>
                        <td>{currency(obligacion.importeFacturado, moneda)}</td>
                        <td>{currency(obligacion.pendienteFacturar, moneda)}</td>
                        <td>{currency(obligacion.importeCobrado, moneda)}</td>
                        <td>{currency(obligacion.facturadoPendienteCobro, moneda)}</td>
                        <td>{currency(obligacion.pendienteTotal, moneda)}</td>
                        <td><span className="status-pill">{estadoLabel[obligacion.estadoFacturacion] || obligacion.estadoFacturacion}</span></td>
                        <td><span className="status-pill">{estadoLabel[obligacion.estadoCobranza] || obligacion.estadoCobranza}</span></td>
                        <td>
                          <button className="btn-secondary btn-small" type="button" onClick={() => setSelectedObligacion(obligacion)}>
                            Ver detalle
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="empty-state">La Via 1 no tiene obligaciones de plan para consultar.</p>
            )}
          </>
        )}
      </div>

      {selectedObligacion && (
        <SituacionVia1DetalleModal
          acuerdoId={acuerdoId}
          obligacion={selectedObligacion}
          monedaCodigo={moneda}
          onClose={() => setSelectedObligacion(null)}
        />
      )}
    </>
  );
};

export default SituacionVia1Panel;

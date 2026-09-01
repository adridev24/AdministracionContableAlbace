import { useEffect, useState } from 'react';
import acuerdosService from '../services/acuerdosService';

const currency = (value, moneda = 'ARS') =>
  `${moneda ? `${moneda} ` : ''}${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const formatDate = (value) => {
  if (!value) return '-';
  const [year, month, day] = String(value).slice(0, 10).split('-');
  return year && month && day ? `${day}/${month}/${year}` : '-';
};

const SituacionVia1DetalleModal = ({ acuerdoId, obligacion, monedaCodigo, onClose }) => {
  const [facturas, setFacturas] = useState([]);
  const [cobranzas, setCobranzas] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!acuerdoId || !obligacion) return;

    let cancelled = false;
    setLoading(true);
    setError('');

    Promise.all([
      acuerdosService.getSituacionVia1Facturas(acuerdoId, obligacion.obligacionId),
      acuerdosService.getSituacionVia1Cobranzas(acuerdoId, obligacion.obligacionId),
    ])
      .then(([facturasData, cobranzasData]) => {
        if (cancelled) return;
        setFacturas(facturasData || []);
        setCobranzas(cobranzasData || []);
      })
      .catch(() => {
        if (!cancelled) setError('No se pudo cargar la trazabilidad de la obligacion.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [acuerdoId, obligacion]);

  if (!obligacion) return null;

  return (
    <div className="modal-backdrop" role="presentation">
      <div className="modal-card situacion-modal" role="dialog" aria-modal="true" aria-label="Trazabilidad de obligacion Via 1">
        <div className="modal-header">
          <div>
            <h2>{obligacion.tipo} {obligacion.numero}</h2>
            <p>Vencimiento {formatDate(obligacion.fechaVencimiento)}</p>
          </div>
          <button className="modal-close" type="button" onClick={onClose} aria-label="Cerrar">x</button>
        </div>

        {loading && <p className="empty-state">Cargando trazabilidad...</p>}
        {error && <p className="form-error">{error}</p>}

        {!loading && !error && (
          <div className="modal-body">
            <section>
              <h3>Facturas asociadas</h3>
              {facturas.length ? (
                <div className="table-wrapper">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Comprobante</th>
                        <th>Fecha</th>
                        <th>Estado</th>
                        <th>Total factura</th>
                        <th>Aplicado</th>
                      </tr>
                    </thead>
                    <tbody>
                      {facturas.map((factura) => (
                        <tr key={`${factura.ventaId}-${factura.importeAplicadoObligacion}`}>
                          <td>{factura.comprobante}</td>
                          <td>{formatDate(factura.fecha)}</td>
                          <td>{factura.estado}</td>
                          <td>{currency(factura.totalFactura, monedaCodigo)}</td>
                          <td>{currency(factura.importeAplicadoObligacion, monedaCodigo)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="empty-state">No hay facturas vinculadas a esta obligacion.</p>
              )}
            </section>

            <section>
              <h3>Cobranzas asociadas</h3>
              {cobranzas.length ? (
                <div className="table-wrapper">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>Fecha</th>
                        <th>Estado</th>
                        <th>Factura</th>
                        <th>Aplicado</th>
                        <th>Medios</th>
                      </tr>
                    </thead>
                    <tbody>
                      {cobranzas.map((cobranza) => (
                        <tr key={`${cobranza.cobranzaId}-${cobranza.ventaId}-${cobranza.importeAplicadoObligacion}`}>
                          <td>{formatDate(cobranza.fecha)}</td>
                          <td>{cobranza.estado}</td>
                          <td>{cobranza.comprobanteFactura}</td>
                          <td>{currency(cobranza.importeAplicadoObligacion, monedaCodigo)}</td>
                          <td>{cobranza.mediosPago || '-'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="empty-state">No hay cobranzas aplicadas a esta obligacion.</p>
              )}
            </section>
          </div>
        )}
      </div>
    </div>
  );
};

export default SituacionVia1DetalleModal;

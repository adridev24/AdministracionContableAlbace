import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import asientosContablesService from '../services/asientosContablesService';
import '../contabilidad.css';

const getApiError = (error) => {
  if (error?.response?.status === 401 || error?.response?.status === 403) return 'No tenes autorizacion para realizar esta accion.';
  if (error?.response?.data?.error) return error.response.data.error;
  if (!error?.response) return 'No se pudo conectar con la API.';
  return 'Ocurrio un error inesperado.';
};

const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatDate = (value) => (value ? new Date(value).toLocaleString('es-AR') : '');

const AsientoDetallePage = () => {
  const { id } = useParams();
  const [asiento, setAsiento] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const fetchAsiento = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await asientosContablesService.getAsiento(id);
      setAsiento(data);
    } catch (fetchError) {
      setError(getApiError(fetchError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    fetchAsiento();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleReversar = async () => {
    const confirmed = window.confirm('Confirma la reversion de este asiento?\n\nSe generara un nuevo asiento con los importes invertidos.\nEl asiento original no sera eliminado ni modificado.');
    if (!confirmed) return;

    setError('');
    setSuccess('');
    try {
      const reversion = await asientosContablesService.reversarAsiento(asiento.id);
      setSuccess(`Asiento reversado correctamente. Nuevo asiento #${reversion.id}.`);
      await fetchAsiento();
    } catch (reversalError) {
      setError(getApiError(reversalError));
    }
  };

  if (loading) {
    return <div className="page-container"><LoadingSpinner /></div>;
  }

  return (
    <div className="page-container contabilidad-page">
      <div className="page-header">
        <div>
          <h1>Detalle de Asiento</h1>
          <p className="page-subtitle">{asiento ? `Asiento #${asiento.id}` : 'Consulta de asiento contable.'}</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/contabilidad/asientos">Asientos Contables</Link>
          {asiento && !asiento.esReversion && asiento.estado !== 'Reversado' && (
            <button className="btn-secondary danger" type="button" onClick={handleReversar}>Reversar</button>
          )}
        </div>
      </div>

      {success && <p className="form-success">{success}</p>}
      {error && <p className="form-error">{error}</p>}

      {asiento && (
        <>
          <SectionCard title="Encabezado">
            <div className="detail-grid">
              <div><span>Fecha</span><strong>{formatDate(asiento.fecha)}</strong></div>
              <div><span>Descripcion</span><strong>{asiento.descripcion}</strong></div>
              <div><span>Tipo</span><strong>{asiento.tipo}</strong></div>
              <div><span>Origen</span><strong>{asiento.moduloOrigen || 'Manual'}</strong></div>
              <div><span>Usuario alta</span><strong>{asiento.usuarioAlta}</strong></div>
              <div><span>Fecha alta</span><strong>{formatDate(asiento.fechaAlta)}</strong></div>
              <div><span>Asiento original</span><strong>{asiento.idAsientoRevertido ? `#${asiento.idAsientoRevertido}` : '-'}</strong></div>
              <div><span>Asiento de reversion</span><strong>{asiento.asientoReversionId ? `#${asiento.asientoReversionId}` : '-'}</strong></div>
            </div>
          </SectionCard>

          <SectionCard title="Detalle">
            <div className="table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Cuenta</th>
                    <th>Descripcion</th>
                    <th>Debe</th>
                    <th>Haber</th>
                  </tr>
                </thead>
                <tbody>
                  {asiento.detalles.map((detalle) => (
                    <tr key={detalle.id}>
                      <td><strong>{detalle.cuentaCodigo}</strong> - {detalle.cuentaNombre}</td>
                      <td>{detalle.descripcion}</td>
                      <td>{formatMoney(detalle.debe)}</td>
                      <td>{formatMoney(detalle.haber)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan="2"><strong>Totales</strong></td>
                    <td><strong>{formatMoney(asiento.totalDebe)}</strong></td>
                    <td><strong>{formatMoney(asiento.totalHaber)}</strong></td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </SectionCard>
        </>
      )}
    </div>
  );
};

export default AsientoDetallePage;

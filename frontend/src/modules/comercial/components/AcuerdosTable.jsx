import { Link } from 'react-router-dom';
import Badge from '../../../shared/components/Badge';

const formatMoney = (value, moneda = 'ARS') =>
  `${moneda} ${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const formatDate = (value) => {
  if (!value) return '-';
  const [year, month, day] = String(value).slice(0, 10).split('-');
  return year && month && day ? `${day}/${month}/${year}` : '-';
};

const summarizeVias = (vias) => {
  if (!vias?.length) return '-';

  return vias
    .map((via) => `${via.viaOperacion} ${via.modalidadCobro}: ${formatMoney(via.montoActual, via.monedaCodigo)}`)
    .join(' / ');
};

const AcuerdosTable = ({ acuerdos }) => {
  if (!acuerdos || acuerdos.length === 0) {
    return (
      <div className="empty-state empty-state-box">
        <strong>No hay acuerdos para mostrar</strong>
        <p>Selecciona un cliente para consultar acuerdos o crea uno nuevo desde la accion principal.</p>
      </div>
    );
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>Acuerdo</th>
            <th>Cliente</th>
            <th>Obra</th>
            <th>Fecha</th>
            <th>Condiciones</th>
            <th>Estado</th>
            <th>Accion</th>
          </tr>
        </thead>
        <tbody>
          {acuerdos.map((item) => (
            <tr key={item.id}>
              <td><strong>{item.numeroAcuerdo}</strong></td>
              <td>{item.clienteNombre || item.clienteExternoId}</td>
              <td>{item.obraNombre || item.obraExternaId}</td>
              <td>{formatDate(item.fechaAcuerdo)}</td>
              <td>{summarizeVias(item.vias)}</td>
              <td><Badge type={item.estado}>{item.estado}</Badge></td>
              <td><Link className="btn-link" to={`/comercial/${item.id}`}>Detalle</Link></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default AcuerdosTable;

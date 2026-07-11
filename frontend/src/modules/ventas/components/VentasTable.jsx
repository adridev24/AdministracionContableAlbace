const formatDate = (value) => {
  if (!value) return '-';
  return new Date(value).toLocaleDateString();
};

const formatNumber = (value, size) => String(value || 0).padStart(size, '0');

const VentasTable = ({ ventas, onEdit }) => {
  if (!ventas?.length) {
    return <p className="empty-state">No hay ventas para los filtros seleccionados.</p>;
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>Fecha</th>
            <th>Comprobante</th>
            <th>Cliente</th>
            <th>Obra</th>
            <th>Moneda</th>
            <th>Estado</th>
            <th>Alta</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {ventas.map((venta) => (
            <tr key={venta.id}>
              <td>{formatDate(venta.fechaComprobante)}</td>
              <td>
                <strong>{venta.tipoComprobanteDescripcion}</strong>
                <span className="table-subtext">
                  {formatNumber(venta.puntoVenta, 4)}-{formatNumber(venta.numeroComprobante, 8)}
                </span>
              </td>
              <td>{venta.clienteNombre || venta.clienteExternoId}</td>
              <td>{venta.obraNombre || venta.obraExternaId}</td>
              <td>{venta.monedaCodigo}</td>
              <td><span className="status-pill is-draft">{venta.estado}</span></td>
              <td>
                <span>{formatDate(venta.fechaAlta)}</span>
                <span className="table-subtext">{venta.usuarioAlta}</span>
              </td>
              <td>
                <button className="btn-secondary" type="button" onClick={() => onEdit(venta)}>
                  Editar
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default VentasTable;

const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatQuantity = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 0, maximumFractionDigits: 4 });

const VentaDetalleTable = ({ detalles, saving, onEdit, onDelete }) => {
  if (!detalles?.length) return <p className="empty-state">La factura no tiene detalles cargados.</p>;

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>Linea</th>
            <th>Item</th>
            <th>Descripcion en factura</th>
            <th>Unidad</th>
            <th>Cantidad</th>
            <th>Precio</th>
            <th>Desc.</th>
            <th>Neto</th>
            <th>IVA</th>
            <th>Nomenclador</th>
            <th>Total</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {detalles.map((detalle) => (
            <tr key={detalle.id}>
              <td>{detalle.numeroLinea}</td>
              <td>
                <strong>{detalle.codigoItem || '-'}</strong>
                <span className="table-subtext">{detalle.itemFacturableDescripcion || 'Sin item parametrizado'}</span>
                {detalle.categoriaItemFacturableDescripcion && <span className="table-subtext">{detalle.categoriaItemFacturableDescripcion}</span>}
              </td>
              <td>
                <strong>{detalle.descripcion}</strong>
                {detalle.observaciones && <span className="table-subtext">{detalle.observaciones}</span>}
              </td>
              <td>{detalle.unidadMedidaAbreviatura || detalle.unidadMedidaDescripcion || '-'}</td>
              <td>{formatQuantity(detalle.cantidad)}</td>
              <td>{formatMoney(detalle.precioUnitario)}</td>
              <td>{formatMoney(detalle.importeDescuento)}</td>
              <td>{formatMoney(detalle.neto)}</td>
              <td>
                {formatMoney(detalle.importeIva)}
                <span className="table-subtext">{Number(detalle.porcentajeIvaAplicado || 0).toLocaleString('es-AR')}%</span>
              </td>
              <td>{detalle.nomencladorCodigo || '-'}</td>
              <td><strong>{formatMoney(detalle.totalLinea)}</strong></td>
              <td>
                <div className="row-actions">
                  <button className="btn-secondary" type="button" onClick={() => onEdit(detalle)} disabled={saving}>Editar</button>
                  <button className="btn-secondary" type="button" onClick={() => onDelete(detalle)} disabled={saving}>Eliminar</button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default VentaDetalleTable;

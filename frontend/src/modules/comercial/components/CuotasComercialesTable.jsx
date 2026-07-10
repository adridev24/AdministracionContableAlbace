import React from 'react';
import Badge from '../../../shared/components/Badge';

const CuotasComercialesTable = ({ cuotas, onAdjustCuota }) => {
  if (!cuotas || cuotas.length === 0) {
    return <p className="empty-state">No existen cuotas comerciales registradas.</p>;
  }

  return (
    <div className="table-wrapper">
      <table className="data-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Tipo</th>
            <th>Vencimiento</th>
            <th>Importe original</th>
            <th>Importe pagado</th>
            <th>Saldo pendiente</th>
            <th>Estado</th>
            <th>Acciones</th>
          </tr>
        </thead>
        <tbody>
          {cuotas.map((cuota) => (
            <tr key={cuota.id}>
              <td>{cuota.numeroCuota}</td>
              <td>{cuota.tipoCuota}</td>
              <td>{new Date(cuota.fechaVencimiento).toLocaleDateString()}</td>
              <td>{cuota.importeOriginal.toLocaleString()}</td>
              <td>{cuota.importePagado.toLocaleString()}</td>
              <td>{cuota.saldoPendiente.toLocaleString()}</td>
              <td><Badge type={cuota.estado}>{cuota.estado}</Badge></td>
              <td>
                {onAdjustCuota && cuota.estado !== 'Pagada' ? (
                  <button className="btn-secondary btn-small" type="button" onClick={() => onAdjustCuota(cuota)}>
                    Ajustar
                  </button>
                ) : (
                  <span className="small-text">-</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default CuotasComercialesTable;

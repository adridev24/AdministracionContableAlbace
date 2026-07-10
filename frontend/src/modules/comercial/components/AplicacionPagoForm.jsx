import React, { useMemo } from 'react';

const formatMoney = (value, monedaCodigo = 'ARS') =>
  `${monedaCodigo} ${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const AplicacionPagoForm = ({ cuotas, monedaCodigo = 'ARS', onChange, pagoTotal }) => {
  const defaultAplicaciones = useMemo(
    () => cuotas?.map((cuota) => ({ cuotaComercialId: cuota.id, importeAplicado: 0 })) || [],
    [cuotas]
  );

  const [aplicaciones, setAplicaciones] = React.useState(defaultAplicaciones);

  React.useEffect(() => {
    setAplicaciones(defaultAplicaciones);
  }, [defaultAplicaciones]);

  const handleChange = (cuotaId, value) => {
    const importeAplicado = Number(value);
    const updated = aplicaciones.map((item) =>
      item.cuotaComercialId === cuotaId ? { ...item, importeAplicado } : item
    );

    const totalAplicado = updated.reduce((sum, item) => sum + item.importeAplicado, 0);
    if (totalAplicado <= pagoTotal) {
      setAplicaciones(updated);
      onChange(updated);
    }
  };

  return (
    <div className="aplicaciones-table">
      <div className="table-wrapper">
        <table className="data-table">
          <thead>
            <tr>
              <th>Cuota</th>
              <th>Saldo pendiente</th>
              <th>Importe aplicacion</th>
            </tr>
          </thead>
          <tbody>
            {cuotas?.map((cuota) => {
              const item = aplicaciones.find((row) => row.cuotaComercialId === cuota.id) || { importeAplicado: 0 };
              return (
                <tr key={cuota.id}>
                  <td>{cuota.numeroCuota} - {cuota.tipoCuota}</td>
                  <td>{formatMoney(cuota.saldoPendiente, monedaCodigo)}</td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      max={cuota.saldoPendiente}
                      step="0.01"
                      value={item.importeAplicado}
                      onChange={(e) => handleChange(cuota.id, e.target.value)}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <p className="hint-text">No se puede aplicar mas que el saldo de cada cuota ni mas que el total disponible del pago.</p>
    </div>
  );
};

export default AplicacionPagoForm;

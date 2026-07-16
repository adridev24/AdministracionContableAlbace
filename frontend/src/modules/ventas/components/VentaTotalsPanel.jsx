const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const VentaTotalsPanel = ({ venta }) => {
  const rows = [
    ['Subtotal bruto', venta?.subtotalBruto],
    ['Descuentos', venta?.totalDescuentos],
    ['Neto gravado', venta?.netoGravado],
    ['Exento', venta?.totalExento],
    ['No gravado', venta?.totalNoGravado],
    ['IVA', venta?.totalIva],
    ['Total', venta?.total],
  ];

  return (
    <div className="totals-panel">
      {rows.map(([label, value]) => (
        <div className={label === 'Total' ? 'is-total' : ''} key={label}>
          <span>{label}</span>
          <strong>{formatMoney(value)}</strong>
        </div>
      ))}
    </div>
  );
};

export default VentaTotalsPanel;

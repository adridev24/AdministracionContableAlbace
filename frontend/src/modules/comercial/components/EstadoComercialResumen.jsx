const EstadoComercialResumen = ({ estado }) => {
  if (!estado) {
    return null;
  }

  const porcentajePagado = estado.totalPrometido > 0
    ? Math.min((estado.totalPagado / estado.totalPrometido) * 100, 100)
    : 0;

  const cards = [
    { label: 'Total comprometido', value: estado.totalPrometido, tone: 'primary' },
    { label: 'Total pagado', value: estado.totalPagado, tone: 'success' },
    { label: 'Saldo pendiente', value: estado.saldoRestante, tone: 'warning' }
  ];

  return (
    <div className="commercial-summary">
      <div className="summary-progress">
        <div>
          <span>Avance de cobro</span>
          <strong>{porcentajePagado.toFixed(0)}%</strong>
        </div>
        <div className="progress-track" aria-hidden="true">
          <span style={{ width: `${porcentajePagado}%` }} />
        </div>
      </div>
      <div className="summary-grid">
        {cards.map((card) => (
          <div key={card.label} className={`summary-card ${card.tone}`}>
            <span>{card.label}</span>
            <strong>${card.value.toLocaleString()}</strong>
          </div>
        ))}
      </div>
    </div>
  );
};

export default EstadoComercialResumen;

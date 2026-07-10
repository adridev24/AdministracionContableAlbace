import React from 'react';

const badgeStyles = {
  base: {
    display: 'inline-flex',
    padding: '4px 10px',
    borderRadius: '999px',
    fontSize: '0.8rem',
    fontWeight: 600,
    textTransform: 'capitalize'
  },
  states: {
    pendiente: { background: '#F9D6A5', color: '#8A5E19' },
    parcial: { background: '#F3E5B4', color: '#7D6100' },
    pagada: { background: '#C6E7C9', color: '#236B1C' },
    vencida: { background: '#F8D7DA', color: '#842029' },
    anulada: { background: '#D6D8DB', color: '#495057' },
    borrador: { background: '#E2E8F0', color: '#334155' },
    aprobado: { background: '#DBEAFE', color: '#1D4ED8' },
    encurso: { background: '#DCFCE7', color: '#166534' },
    finalizado: { background: '#EDE9FE', color: '#5B21B6' }
  }
};

const Badge = ({ children, type }) => {
  const style = badgeStyles.states[(type || '').toLowerCase()] || {
    background: '#E3E7EE',
    color: '#23303F'
  };

  return (
    <span style={{ ...badgeStyles.base, ...style }}>
      {children}
    </span>
  );
};

export default Badge;

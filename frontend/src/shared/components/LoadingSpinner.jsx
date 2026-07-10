import React from 'react';

const LoadingSpinner = () => (
  <div style={{ textAlign: 'center', padding: '40px 0' }}>
    <div className="loading-dot-wrapper">
      <span className="loading-dot" />
      <span className="loading-dot" />
      <span className="loading-dot" />
    </div>
    <p style={{ color: '#5E6C84', marginTop: '14px' }}>Cargando información...</p>
  </div>
);

export default LoadingSpinner;

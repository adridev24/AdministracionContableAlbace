import React, { useEffect, useState } from 'react';
import '../comercial.css';

const parseDateValue = (value) => {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toISOString().slice(0, 10);
};

const AgregarCuotaModal = ({ open, planPago, onClose, onSave, loading, error }) => {
  const [importeOriginal, setImporteOriginal] = useState('');
  const [fechaVencimiento, setFechaVencimiento] = useState('');
  const [tipoCuota, setTipoCuota] = useState('Adicional');
  const [motivo, setMotivo] = useState('');
  const [localError, setLocalError] = useState('');

  useEffect(() => {
    if (!open || !planPago) {
      return;
    }

    setImporteOriginal('');
    setFechaVencimiento(parseDateValue(planPago.fechaPrimerVencimiento));
    setTipoCuota('Adicional');
    setMotivo('');
    setLocalError('');
  }, [open, planPago]);

  if (!open) {
    return null;
  }

  const handleSubmit = () => {
    setLocalError('');

    const importeValue = Number(importeOriginal);
    if (Number.isNaN(importeValue) || importeValue <= 0) {
      setLocalError('El importe original debe ser mayor que cero.');
      return;
    }

    if (!fechaVencimiento) {
      setLocalError('La fecha de vencimiento es obligatoria.');
      return;
    }

    if (!motivo.trim()) {
      setLocalError('El motivo es obligatorio.');
      return;
    }

    onSave({
      importeOriginal: importeValue,
      fechaVencimiento: new Date(fechaVencimiento).toISOString(),
      tipoCuota,
      motivo: motivo.trim()
    });
  };

  return (
    <div className="modal-backdrop">
      <div className="modal-card">
        <div className="modal-header">
          <h2>Agregar cuota al plan</h2>
          <button type="button" className="modal-close" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          <div className="form-row">
            <label>Tipo de cuota</label>
            <select value={tipoCuota} onChange={(e) => setTipoCuota(e.target.value)}>
              <option value="Adicional">Adicional</option>
              <option value="Ajuste">Ajuste</option>
            </select>
          </div>
          <div className="form-row">
            <label>Importe original</label>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={importeOriginal}
              onChange={(e) => setImporteOriginal(e.target.value)}
            />
          </div>
          <div className="form-row">
            <label>Fecha de vencimiento</label>
            <input
              type="date"
              value={fechaVencimiento}
              onChange={(e) => setFechaVencimiento(e.target.value)}
            />
          </div>
          <div className="form-row">
            <label>Motivo</label>
            <textarea
              rows="3"
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          </div>
          <div className="alert-box">
            <strong>Advertencia:</strong> Este ajuste solo afecta el saldo comercial. No genera factura ni asiento contable.
          </div>

          {localError && <p className="form-error">{localError}</p>}
          {error && <p className="form-error">{error}</p>}

          <div className="modal-footer">
            <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
            <button type="button" className="btn-primary" onClick={handleSubmit} disabled={loading}>
              {loading ? 'Guardando...' : 'Agregar cuota'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AgregarCuotaModal;

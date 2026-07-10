import React, { useEffect, useState } from 'react';
import acuerdosService from '../services/acuerdosService';
import '../comercial.css';

const parseDateValue = (value) => {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return date.toISOString().slice(0, 10);
};

const AjusteCuotaModal = ({ open, cuota, onClose, onSave, loading, error }) => {
  const [nuevoImporte, setNuevoImporte] = useState('');
  const [nuevaFecha, setNuevaFecha] = useState('');
  const [motivo, setMotivo] = useState('');
  const [historial, setHistorial] = useState([]);
  const [historialLoading, setHistorialLoading] = useState(false);
  const [localError, setLocalError] = useState('');

  useEffect(() => {
    if (!open || !cuota) {
      return;
    }

    setNuevoImporte(cuota.importeOriginal.toString());
    setNuevaFecha(parseDateValue(cuota.fechaVencimiento));
    setMotivo('');
    setLocalError('');
    setHistorial([]);
    setHistorialLoading(true);

    acuerdosService.getHistorialAjustesCuota(cuota.id)
      .then((data) => setHistorial(data || []))
      .catch(() => setHistorial([]))
      .finally(() => setHistorialLoading(false));
  }, [open, cuota]);

  if (!open || !cuota) {
    return null;
  }

  const handleSubmit = () => {
    setLocalError('');
    if (!motivo.trim()) {
      setLocalError('El motivo es obligatorio.');
      return;
    }

    const importeValue = Number(nuevoImporte);
    if (Number.isNaN(importeValue) || importeValue <= 0) {
      setLocalError('El nuevo importe debe ser un número mayor que cero.');
      return;
    }

    if (importeValue < cuota.importePagado) {
      setLocalError('El nuevo importe no puede ser menor al importe ya pagado.');
      return;
    }

    if (!nuevaFecha) {
      setLocalError('La fecha de vencimiento es obligatoria.');
      return;
    }

    onSave({
      nuevoImporteOriginal: importeValue,
      nuevaFechaVencimiento: new Date(nuevaFecha).toISOString(),
      motivo: motivo.trim()
    });
  };

  return (
    <div className="modal-backdrop">
      <div className="modal-card">
        <div className="modal-header">
          <h2>Ajustar cuota #{cuota.numeroCuota}</h2>
          <button type="button" className="modal-close" onClick={onClose}>×</button>
        </div>

        <div className="modal-body">
          <div className="info-grid">
            <div>
              <strong>Importe actual</strong>
              <p>${Number(cuota.importeOriginal).toLocaleString()}</p>
            </div>
            <div>
              <strong>Pagado</strong>
              <p>${Number(cuota.importePagado).toLocaleString()}</p>
            </div>
            <div>
              <strong>Saldo pendiente</strong>
              <p>${Number(cuota.saldoPendiente).toLocaleString()}</p>
            </div>
            <div>
              <strong>Estado</strong>
              <p>{cuota.estado}</p>
            </div>
          </div>

          <div className="form-row">
            <label>Nuevo importe original</label>
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={nuevoImporte}
              onChange={(e) => setNuevoImporte(e.target.value)}
            />
          </div>
          <div className="form-row">
            <label>Nueva fecha de vencimiento</label>
            <input
              type="date"
              value={nuevaFecha}
              onChange={(e) => setNuevaFecha(e.target.value)}
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
              {loading ? 'Guardando...' : 'Guardar ajuste'}
            </button>
          </div>

          <div className="history-section">
            <h3>Historial de ajustes</h3>
            {historialLoading ? (
              <p>Cargando historial...</p>
            ) : historial.length === 0 ? (
              <p className="empty-state">No hay ajustes registrados para esta cuota.</p>
            ) : (
              <div className="history-list">
                {historial.map((item) => (
                  <div key={item.id} className="history-item">
                    <p><strong>{new Date(item.fechaAjuste).toLocaleString()}</strong> - {item.tipoAjuste}</p>
                    <p>{item.motivo}</p>
                    <p>Usuario: {item.usuarioAjuste}</p>
                    <p>
                      {item.importeAnterior !== null && item.importeNuevo !== null && (
                        <>Importe {item.importeAnterior.toLocaleString()} → {item.importeNuevo.toLocaleString()}</>
                      )}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default AjusteCuotaModal;

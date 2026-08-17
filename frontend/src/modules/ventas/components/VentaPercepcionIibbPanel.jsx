import { useEffect, useState } from 'react';

const emptyConfig = {
  situacion: 'Pendiente',
  regimenPercepcionIibbId: '',
  numeroInscripcionIibb: '',
  jurisdiccionIibb: 'Entre Rios',
  exclusionDesde: '',
  exclusionHasta: '',
  motivoExclusion: '',
  observaciones: '',
};

const formatMoney = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatPercent = (value) => `${Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}%`;
const toDateInput = (value) => (value ? new Date(value).toISOString().slice(0, 10) : '');

const VentaPercepcionIibbPanel = ({
  venta,
  percepcion,
  clienteConfig,
  regimenes,
  saving,
  calculating,
  readOnly = false,
  onSaveConfig,
  onCalcular,
}) => {
  const [form, setForm] = useState(emptyConfig);

  useEffect(() => {
    let mounted = true;
    const next = !clienteConfig ? emptyConfig : {
      situacion: clienteConfig.situacion || 'Pendiente',
      regimenPercepcionIibbId: clienteConfig.regimenPercepcionIibbId ? String(clienteConfig.regimenPercepcionIibbId) : '',
      numeroInscripcionIibb: clienteConfig.numeroInscripcionIibb || '',
      jurisdiccionIibb: clienteConfig.jurisdiccionIibb || 'Entre Rios',
      exclusionDesde: toDateInput(clienteConfig.exclusionDesde),
      exclusionHasta: toDateInput(clienteConfig.exclusionHasta),
      motivoExclusion: clienteConfig.motivoExclusion || '',
      observaciones: clienteConfig.observaciones || '',
    };

    Promise.resolve().then(() => {
      if (mounted) setForm(next);
    });

    return () => { mounted = false; };
  }, [clienteConfig]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({
      ...prev,
      [name]: value,
      ...(name === 'situacion' && value !== 'Alcanzado' ? { regimenPercepcionIibbId: '' } : {}),
    }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    onSaveConfig({
      clienteExternoId: venta.clienteExternoId,
      situacion: form.situacion,
      regimenPercepcionIibbId: form.regimenPercepcionIibbId ? Number(form.regimenPercepcionIibbId) : null,
      numeroInscripcionIibb: form.numeroInscripcionIibb.trim() || null,
      jurisdiccionIibb: form.jurisdiccionIibb.trim() || null,
      exclusionDesde: form.exclusionDesde || null,
      exclusionHasta: form.exclusionHasta || null,
      motivoExclusion: form.motivoExclusion.trim() || null,
      observaciones: form.observaciones.trim() || null,
    });
  };

  return (
    <div className="ventas-tributos-panel">
      <form className="venta-form" onSubmit={handleSubmit}>
        <div className="form-grid">
          <div className="form-field">
            <label>Situacion del cliente</label>
            <select name="situacion" value={form.situacion} onChange={handleChange} disabled={readOnly}>
              <option value="Pendiente">Pendiente</option>
              <option value="NoAlcanzado">No alcanzado</option>
              <option value="Alcanzado">Alcanzado</option>
              <option value="Excluido">Excluido</option>
            </select>
          </div>
          <div className="form-field">
            <label>Regimen</label>
            <select name="regimenPercepcionIibbId" value={form.regimenPercepcionIibbId} onChange={handleChange} disabled={readOnly || form.situacion !== 'Alcanzado'}>
              <option value="">Seleccionar</option>
              {(regimenes || []).map((regimen) => (
                <option key={regimen.id} value={regimen.id}>{regimen.codigo} - {regimen.descripcion}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>Inscripcion IIBB</label>
            <input name="numeroInscripcionIibb" value={form.numeroInscripcionIibb} onChange={handleChange} disabled={readOnly} />
          </div>
          <div className="form-field">
            <label>Jurisdiccion</label>
            <input name="jurisdiccionIibb" value={form.jurisdiccionIibb} onChange={handleChange} disabled={readOnly} />
          </div>
          {form.situacion === 'Excluido' && (
            <>
              <div className="form-field">
                <label>Exclusion desde</label>
                <input name="exclusionDesde" type="date" value={form.exclusionDesde} onChange={handleChange} disabled={readOnly} />
              </div>
              <div className="form-field">
                <label>Exclusion hasta</label>
                <input name="exclusionHasta" type="date" value={form.exclusionHasta} onChange={handleChange} disabled={readOnly} />
              </div>
              <div className="form-field full-width">
                <label>Motivo exclusion</label>
                <input name="motivoExclusion" value={form.motivoExclusion} onChange={handleChange} disabled={readOnly} />
              </div>
            </>
          )}
          <div className="form-field full-width">
            <label>Observaciones tributarias</label>
            <textarea name="observaciones" rows="2" value={form.observaciones} onChange={handleChange} disabled={readOnly} />
          </div>
        </div>
        <div className="form-actions">
          <button className="btn-secondary" type="submit" disabled={saving || readOnly}>Guardar configuracion</button>
          <button className="btn-primary" type="button" onClick={onCalcular} disabled={saving || calculating || readOnly}>
            {calculating ? 'Calculando...' : percepcion ? 'Recalcular percepcion' : 'Calcular percepcion'}
          </button>
        </div>
      </form>

      <div className="totals-panel">
        <div>
          <span>Estado</span>
          <strong>{percepcion?.resultado || (clienteConfig ? 'Sin calcular' : 'Cliente sin configurar')}</strong>
        </div>
        <div>
          <span>Regimen</span>
          <strong>{percepcion?.codigoRegimenAplicado || '-'}</strong>
        </div>
        <div>
          <span>Base imponible</span>
          <strong>{formatMoney(percepcion?.baseImponible)}</strong>
        </div>
        <div>
          <span>Alicuota</span>
          <strong>{formatPercent(percepcion?.alicuotaAplicada)}</strong>
        </div>
        <div className="is-total">
          <span>Percepcion IIBB ER</span>
          <strong>{formatMoney(percepcion?.importe)}</strong>
        </div>
      </div>

      {venta?.percepcionIibbRequiereRecalculo && <p className="form-warning">La percepcion requiere recalculo porque cambiaron datos de la factura.</p>}
      {percepcion?.motivo && <p className="field-help">{percepcion.motivo}</p>}
    </div>
  );
};

export default VentaPercepcionIibbPanel;

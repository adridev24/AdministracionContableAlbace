import { useEffect, useMemo, useRef, useState } from 'react';
import ClienteObraSelector from '../../comercial/components/ClienteObraSelector';
import ventasService from '../services/ventasService';

const initialForm = {
  tipoComprobanteVentaId: '',
  puntoVentaComprobanteId: '',
  clienteExternoId: '',
  obraExternoId: '',
  fechaComprobante: new Date().toISOString().slice(0, 10),
  puntoVenta: '',
  numeroComprobante: '',
  monedaCodigo: 'ARS',
  cotizacion: '1',
  observaciones: '',
};

const toDateInput = (value) => {
  if (!value) return new Date().toISOString().slice(0, 10);
  return new Date(value).toISOString().slice(0, 10);
};

const buildInitialForm = (selectedVenta) => {
  if (!selectedVenta) return initialForm;

  return {
    tipoComprobanteVentaId: String(selectedVenta.tipoComprobanteVentaId || ''),
    puntoVentaComprobanteId: String(selectedVenta.puntoVentaComprobanteId || ''),
    clienteExternoId: String(selectedVenta.clienteExternoId || ''),
    obraExternoId: String(selectedVenta.obraExternaId || ''),
    fechaComprobante: toDateInput(selectedVenta.fechaComprobante),
    puntoVenta: String(selectedVenta.puntoVenta || ''),
    numeroComprobante: String(selectedVenta.numeroComprobante || ''),
    monedaCodigo: selectedVenta.monedaCodigo || 'ARS',
    cotizacion: String(selectedVenta.cotizacion || 1),
    observaciones: selectedVenta.observaciones || '',
  };
};

const VentaHeaderForm = ({ tiposComprobante, selectedVenta, saving, onCancel, onSubmit }) => {
  const [form, setForm] = useState(() => buildInitialForm(selectedVenta));
  const [puntosVenta, setPuntosVenta] = useState([]);
  const [loadingPuntos, setLoadingPuntos] = useState(false);
  const [puntosError, setPuntosError] = useState('');
  const requestRef = useRef(0);

  const isEditing = Boolean(selectedVenta?.id);
  const activeTipos = useMemo(() => tiposComprobante.filter((tipo) => tipo.activo), [tiposComprobante]);
  const selectedPunto = useMemo(
    () => puntosVenta.find((punto) => String(punto.puntoVentaComprobanteId) === form.puntoVentaComprobanteId),
    [puntosVenta, form.puntoVentaComprobanteId],
  );

  useEffect(() => {
    const tipoId = form.tipoComprobanteVentaId;
    const requestId = requestRef.current + 1;
    requestRef.current = requestId;

    Promise.resolve().then(() => {
      if (requestRef.current !== requestId) return;
      setPuntosError('');

      if (!tipoId) {
        setPuntosVenta([]);
        setLoadingPuntos(false);
        setForm((prev) => ({ ...prev, puntoVentaComprobanteId: '', puntoVenta: '' }));
        return;
      }

      setLoadingPuntos(true);
      ventasService.getPuntosVentaPorComprobante(
        tipoId,
        selectedVenta?.puntoVentaComprobanteId ? { relacionActualId: selectedVenta.puntoVentaComprobanteId } : {},
      )
        .then((data) => {
          if (requestRef.current !== requestId) return;
          const nextPuntos = data || [];
          setPuntosVenta(nextPuntos);
          setForm((prev) => {
            const current = nextPuntos.find((punto) => String(punto.puntoVentaComprobanteId) === prev.puntoVentaComprobanteId);
            const matchingLegacyPoint = !prev.puntoVentaComprobanteId && prev.puntoVenta
              ? nextPuntos.find((punto) => punto.numero === Number(prev.puntoVenta))
              : null;
            const autoSelected = nextPuntos.length === 1 ? nextPuntos[0] : null;
            const selected = current || matchingLegacyPoint || autoSelected;

            return {
              ...prev,
              puntoVentaComprobanteId: selected ? String(selected.puntoVentaComprobanteId) : '',
              puntoVenta: selected ? String(selected.numero) : '',
            };
          });
        })
        .catch((error) => {
          if (requestRef.current !== requestId) return;
          setPuntosVenta([]);
          setPuntosError(error?.response?.data?.error || 'No se pudieron cargar los puntos de venta habilitados.');
          setForm((prev) => ({ ...prev, puntoVentaComprobanteId: '', puntoVenta: '' }));
        })
        .finally(() => {
          if (requestRef.current === requestId) setLoadingPuntos(false);
        });
    });

    return undefined;
  }, [form.tipoComprobanteVentaId, selectedVenta?.puntoVentaComprobanteId]);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'monedaCodigo' && value.trim().toUpperCase() === 'ARS') {
        next.cotizacion = '1';
      }
      if (name === 'tipoComprobanteVentaId') {
        next.puntoVentaComprobanteId = '';
        next.puntoVenta = '';
      }
      if (name === 'puntoVentaComprobanteId') {
        const punto = puntosVenta.find((item) => String(item.puntoVentaComprobanteId) === value);
        next.puntoVenta = punto ? String(punto.numero) : '';
      }
      return next;
    });
  };

  const handleClienteObraChange = (values) => {
    setForm((prev) => ({
      ...prev,
      clienteExternoId: values.clienteExternoId ?? prev.clienteExternoId,
      obraExternoId: values.obraExternoId ?? values.obraExternaId ?? '',
    }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    if (!form.puntoVentaComprobanteId) {
      setPuntosError('Seleccione un punto de venta habilitado para el comprobante indicado.');
      return;
    }
    if (selectedPunto && !selectedPunto.habilitado) {
      setPuntosError('Seleccione una combinacion activa antes de guardar.');
      return;
    }

    onSubmit({
      tipoComprobanteVentaId: Number(form.tipoComprobanteVentaId),
      puntoVentaComprobanteId: form.puntoVentaComprobanteId ? Number(form.puntoVentaComprobanteId) : null,
      clienteExternoId: String(form.clienteExternoId),
      obraExternaId: String(form.obraExternoId),
      fechaComprobante: form.fechaComprobante,
      puntoVenta: Number(form.puntoVenta),
      numeroComprobante: Number(form.numeroComprobante),
      monedaCodigo: form.monedaCodigo.trim().toUpperCase(),
      cotizacion: Number(form.cotizacion),
      observaciones: form.observaciones,
    });
  };

  return (
    <form className="venta-form" onSubmit={handleSubmit}>
      <div className="form-grid">
        <div className="form-field">
          <label>Tipo de comprobante</label>
          <select
            name="tipoComprobanteVentaId"
            value={form.tipoComprobanteVentaId}
            onChange={handleChange}
            required
          >
            <option value="">Seleccionar</option>
            {activeTipos.map((tipo) => (
              <option key={tipo.id} value={tipo.id}>
                {tipo.descripcion}
              </option>
            ))}
          </select>
        </div>

        <div className="form-field">
          <label>Punto de venta</label>
          <select
            name="puntoVentaComprobanteId"
            value={form.puntoVentaComprobanteId}
            onChange={handleChange}
            disabled={!form.tipoComprobanteVentaId || loadingPuntos || !puntosVenta.length}
            required
          >
            <option value="">
              {!form.tipoComprobanteVentaId ? 'Seleccione primero un comprobante' : loadingPuntos ? 'Cargando...' : 'Seleccionar'}
            </option>
            {puntosVenta.map((punto) => (
              <option key={punto.puntoVentaComprobanteId} value={punto.puntoVentaComprobanteId}>
                {punto.habilitado ? punto.textoMostrar : `${punto.textoMostrar} (no habilitado)`}
              </option>
            ))}
          </select>
          <div className="field-help">
            {form.tipoComprobanteVentaId
              ? 'Se muestran unicamente los puntos habilitados para el comprobante seleccionado.'
              : 'Seleccione primero un tipo de comprobante.'}
          </div>
          {form.tipoComprobanteVentaId && !loadingPuntos && !puntosVenta.length && !puntosError && (
            <p className="form-warning">No existen puntos de venta activos habilitados para el comprobante seleccionado. Revise la Parametrizacion de Ventas.</p>
          )}
          {selectedPunto && !selectedPunto.habilitado && (
            <p className="form-warning">El punto de venta actualmente asignado ya no esta habilitado para este comprobante. Seleccione una combinacion activa antes de guardar.</p>
          )}
          {puntosError && <p className="form-error">{puntosError}</p>}
        </div>

      </div>

      <ClienteObraSelector
        clienteExternoId={form.clienteExternoId}
        obraExternoId={form.obraExternoId}
        onChange={handleClienteObraChange}
      />

      <div className="form-grid">
        <div className="form-field">
          <label>Fecha</label>
          <input name="fechaComprobante" type="date" value={form.fechaComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Numero</label>
          <input name="numeroComprobante" type="number" min="1" value={form.numeroComprobante} onChange={handleChange} required />
        </div>

        <div className="form-field">
          <label>Moneda</label>
          <select name="monedaCodigo" value={form.monedaCodigo} onChange={handleChange} required>
            <option value="ARS">ARS</option>
            <option value="USD">USD</option>
          </select>
        </div>

        <div className="form-field">
          <label>Cotizacion</label>
          <input
            name="cotizacion"
            type="number"
            min="0.000001"
            step="0.000001"
            value={form.cotizacion}
            onChange={handleChange}
            disabled={form.monedaCodigo === 'ARS'}
            required
          />
        </div>
      </div>

      <div className="form-field">
        <label>Observaciones</label>
        <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
      </div>

      <div className="venta-state-line">
        <span>Estado</span>
        <strong>{selectedVenta?.estado || 'Borrador'}</strong>
      </div>

      <div className="form-actions">
        {isEditing && (
          <button className="btn-secondary" type="button" onClick={onCancel} disabled={saving}>
            Cancelar
          </button>
        )}
        <button className="btn-primary" type="submit" disabled={saving || loadingPuntos || !form.puntoVentaComprobanteId || Boolean(selectedPunto && !selectedPunto.habilitado)}>
          {saving ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear borrador'}
        </button>
      </div>
    </form>
  );
};

export default VentaHeaderForm;

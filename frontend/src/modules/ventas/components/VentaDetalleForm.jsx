import { useEffect, useMemo, useState } from 'react';

const emptyForm = {
  itemFacturableId: '',
  descripcion: '',
  cantidad: '1',
  precioUnitario: '',
  porcentajeDescuento: '0',
  tratamientoIvaId: '',
  nomencladorId: '',
  observaciones: '',
};

const formatMoney = (value) => (
  value === null || value === undefined || value === ''
    ? '-'
    : Number(value).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
);

const VentaDetalleForm = ({ venta, itemsFacturables, alicuotas, nomencladores, selectedDetalle, saving, onCancel, onSubmit }) => {
  const [form, setForm] = useState(emptyForm);
  const [itemSearch, setItemSearch] = useState('');
  const [lastSuggestedDescription, setLastSuggestedDescription] = useState('');
  const isEditing = Boolean(selectedDetalle?.id);
  const isExportacion = Boolean(venta?.tipoComprobanteEsExportacion);
  const requiereNomenclador = Boolean(venta?.tipoComprobanteRequiereNomenclador);

  useEffect(() => {
    let mounted = true;
    const nextForm = !selectedDetalle ? emptyForm : {
      itemFacturableId: String(selectedDetalle.itemFacturableId || ''),
      descripcion: selectedDetalle.descripcion || '',
      cantidad: String(selectedDetalle.cantidad || 1),
      precioUnitario: String(selectedDetalle.precioUnitario ?? ''),
      porcentajeDescuento: String(selectedDetalle.porcentajeDescuento || 0),
      tratamientoIvaId: String(selectedDetalle.tratamientoIvaId || ''),
      nomencladorId: String(selectedDetalle.nomencladorId || ''),
      observaciones: selectedDetalle.observaciones || '',
    };

    Promise.resolve().then(() => {
      if (!mounted) return;
      setForm(nextForm);
      setLastSuggestedDescription(selectedDetalle?.itemFacturableDescripcion || '');
    });

    return () => { mounted = false; };
  }, [selectedDetalle]);

  const activeItems = useMemo(() => (itemsFacturables || []).filter((item) => item.activo), [itemsFacturables]);

  const filteredItems = useMemo(() => {
    const term = itemSearch.trim().toLowerCase();
    if (!term) return activeItems;
    return activeItems.filter((item) =>
      item.codigo?.toLowerCase().includes(term) ||
      item.descripcion?.toLowerCase().includes(term) ||
      item.categoriaDescripcion?.toLowerCase().includes(term));
  }, [activeItems, itemSearch]);

  const selectedItem = useMemo(
    () => activeItems.find((item) => String(item.id) === form.itemFacturableId),
    [activeItems, form.itemFacturableId],
  );

  const compatibleAlicuotas = useMemo(() => {
    return (alicuotas || []).filter((alicuota) => alicuota.activo && (!isExportacion || alicuota.tipoTratamiento !== 'Gravado'));
  }, [alicuotas, isExportacion]);

  const activeNomencladores = useMemo(() => (nomencladores || []).filter((item) => item.activo), [nomencladores]);

  const buildItemSuggestion = (item, previousForm) => {
    const suggestedDescription = item?.descripcion || '';
    const currentDescription = previousForm.descripcion || '';
    const descriptionWasCustomized = currentDescription &&
      currentDescription !== lastSuggestedDescription;
    const replaceDescription = !descriptionWasCustomized || window.confirm('La descripcion fue modificada. Queres reemplazarla por la descripcion del nuevo item?');
    const suggestedIva = item?.tratamientoIvaPredeterminadoId ? String(item.tratamientoIvaPredeterminadoId) : '';
    const ivaCompatible = compatibleAlicuotas.some((alicuota) => String(alicuota.id) === suggestedIva);
    const suggestedNomenclador = item?.nomencladorPredeterminadoId ? String(item.nomencladorPredeterminadoId) : '';
    const nomencladorActivo = activeNomencladores.some((nomenclador) => String(nomenclador.id) === suggestedNomenclador);
    return {
      ...previousForm,
      itemFacturableId: item ? String(item.id) : '',
      descripcion: replaceDescription ? suggestedDescription : previousForm.descripcion,
      tratamientoIvaId: ivaCompatible ? suggestedIva : '',
      nomencladorId: requiereNomenclador && nomencladorActivo ? suggestedNomenclador : '',
      precioUnitario: item?.precioPredeterminado !== null && item?.precioPredeterminado !== undefined
        ? String(item.precioPredeterminado)
        : previousForm.precioUnitario,
    };
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    if (name === 'itemFacturableId') {
      const item = activeItems.find((candidate) => String(candidate.id) === value);
      const nextForm = buildItemSuggestion(item, form);
      setLastSuggestedDescription(item?.descripcion || '');
      setForm(nextForm);
      return;
    }

    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    onSubmit({
      itemFacturableId: form.itemFacturableId ? Number(form.itemFacturableId) : null,
      descripcion: form.descripcion.trim(),
      cantidad: Number(form.cantidad),
      precioUnitario: Number(form.precioUnitario),
      porcentajeDescuento: Number(form.porcentajeDescuento || 0),
      tratamientoIvaId: Number(form.tratamientoIvaId),
      nomencladorId: requiereNomenclador && form.nomencladorId ? Number(form.nomencladorId) : null,
      observaciones: form.observaciones.trim() || null,
    });
  };

  return (
    <form className="venta-form" onSubmit={handleSubmit}>
      {isExportacion && <p className="info-banner">Operacion de exportacion: no se admiten tratamientos de IVA gravados locales.</p>}
      <div className="form-grid">
        <div className="form-field">
          <label>Buscar item</label>
          <input value={itemSearch} onChange={(event) => setItemSearch(event.target.value)} placeholder="Codigo, descripcion o categoria" />
        </div>
        <div className="form-field full-width">
          <label>Item facturable</label>
          <select name="itemFacturableId" value={form.itemFacturableId} onChange={handleChange} required>
            <option value="">{filteredItems.length ? 'Seleccionar' : 'No hay items activos para la busqueda'}</option>
            {filteredItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.codigo} - {item.descripcion}
              </option>
            ))}
          </select>
        </div>

        {selectedItem && (
          <div className="param-summary full-width">
            <span className="tag">{selectedItem.categoriaDescripcion || 'Sin categoria'}</span>
            <span className="tag">{selectedItem.unidadMedidaAbreviatura || selectedItem.unidadMedidaDescripcion}</span>
            <span className="tag">IVA: {selectedItem.tratamientoIvaDescripcion}</span>
            <span className="tag">Nomenclador: {selectedItem.nomencladorDescripcion || '-'}</span>
            <span className="tag">Precio: {formatMoney(selectedItem.precioPredeterminado)}</span>
          </div>
        )}

        <div className="form-field full-width">
          <label>Descripcion en factura</label>
          <input name="descripcion" value={form.descripcion} onChange={handleChange} required />
        </div>
        <div className="form-field">
          <label>Cantidad</label>
          <input name="cantidad" type="number" min="0.0001" step="0.0001" value={form.cantidad} onChange={handleChange} required />
        </div>
        <div className="form-field">
          <label>Precio unitario</label>
          <input name="precioUnitario" type="number" min="0" step="0.0001" value={form.precioUnitario} onChange={handleChange} required />
        </div>
        <div className="form-field">
          <label>Descuento %</label>
          <input name="porcentajeDescuento" type="number" min="0" max="100" step="0.0001" value={form.porcentajeDescuento} onChange={handleChange} />
        </div>
        <div className="form-field">
          <label>Tratamiento IVA</label>
          <select name="tratamientoIvaId" value={form.tratamientoIvaId} onChange={handleChange} required>
            <option value="">Seleccionar</option>
            {compatibleAlicuotas.map((alicuota) => (
              <option key={alicuota.id} value={alicuota.id}>
                {alicuota.descripcion} ({alicuota.tipoTratamiento})
              </option>
            ))}
          </select>
        </div>
        {requiereNomenclador && (
          <div className="form-field">
            <label>Nomenclador FCE</label>
            <select name="nomencladorId" value={form.nomencladorId} onChange={handleChange} required>
              <option value="">Seleccionar</option>
              {activeNomencladores.map((item) => (
                <option key={item.id} value={item.id}>{item.codigo} - {item.descripcion}</option>
              ))}
            </select>
          </div>
        )}
        <div className="form-field full-width">
          <label>Observaciones</label>
          <textarea name="observaciones" rows="3" value={form.observaciones} onChange={handleChange} />
        </div>
      </div>
      <div className="form-actions">
        {isEditing && <button className="btn-secondary" type="button" onClick={onCancel} disabled={saving}>Cancelar</button>}
        <button className="btn-primary" type="submit" disabled={saving}>
          {saving ? 'Guardando...' : isEditing ? 'Guardar linea' : 'Agregar linea'}
        </button>
      </div>
    </form>
  );
};

export default VentaDetalleForm;

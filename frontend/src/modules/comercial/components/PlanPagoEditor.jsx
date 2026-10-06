import { calendarDateValue as parseDateValue, calendarDateToApi } from '../../../shared/utils/calendarDate';
import React, { useEffect, useMemo, useState } from 'react';
import '../comercial.css';

const isAnticipo = (cuota) => cuota.tipoCuota === 'Anticipo';

const money = (value, moneda = '') =>
  `${moneda ? `${moneda} ` : ''}${Number(value || 0).toLocaleString('es-AR', { maximumFractionDigits: 2 })}`;

const PlanPagoEditor = ({ planPago, viaMonto = 0, monedaCodigo = '', onSave, loading, error, readOnly = false }) => {
  const [plan, setPlan] = useState({ ...planPago });
  const [localError, setLocalError] = useState('');

  useEffect(() => {
    setPlan({
      ...planPago,
      fechaAnticipo: parseDateValue(planPago.cuotas.find(isAnticipo)?.fechaVencimiento),
      fechaPrimerVencimiento: parseDateValue(planPago.fechaPrimerVencimiento),
      cuotas: planPago.cuotas.map((cuota) => ({
        ...cuota,
        fechaVencimiento: parseDateValue(cuota.fechaVencimiento),
      })),
    });
    setLocalError('');
  }, [planPago]);

  const totalCuotas = useMemo(() => (
    plan.cuotas
      .filter((cuota) => !isAnticipo(cuota) && cuota.estado !== 'Anulada')
      .reduce((sum, cuota) => sum + Number(cuota.importeOriginal || 0), 0)
  ), [plan]);

  const totalPlan = useMemo(() => (
    Number(plan.tieneAnticipo ? plan.montoAnticipo || 0 : 0) + totalCuotas
  ), [plan.tieneAnticipo, plan.montoAnticipo, totalCuotas]);

  const difference = Number(viaMonto || 0) - totalPlan;
  const totalsMatch = Math.abs(difference) <= 0.01;

  const handlePlanField = (field, value) => {
    setPlan((prev) => ({ ...prev, [field]: value }));
  };

  const handleAnticipoAmount = (value) => {
    setPlan((prev) => ({
      ...prev,
      montoAnticipo: value,
      cuotas: prev.cuotas.map((cuota) => (
        isAnticipo(cuota)
          ? {
              ...cuota,
              importeOriginal: value,
              saldoPendiente: Math.max(Number(value || 0) - Number(cuota.importePagado || 0), 0)
            }
          : cuota
      ))
    }));
  };

  const handleCuotaField = (id, field, value) => {
    setPlan((prev) => ({
      ...prev,
      cuotas: prev.cuotas.map((cuota) => (
        cuota.id === id ? { ...cuota, [field]: value } : cuota
      )),
    }));
  };

  const handleSave = () => {
    if (!totalsMatch) {
      setLocalError('El total del plan no coincide con el monto de la via.');
      return;
    }

    if (readOnly) {
      setLocalError('El plan base no se puede modificar con el acuerdo aprobado.');
      return;
    }

    if (plan.tieneAnticipo && (!parseDateValue(plan.fechaAnticipo) || parseDateValue(plan.fechaAnticipo) === '0001-01-01')) {
      setLocalError('La fecha de vencimiento del anticipo es obligatoria y debe ser válida.');
      return;
    }

    if (!parseDateValue(plan.fechaPrimerVencimiento) || plan.cuotas.some((cuota) => !isAnticipo(cuota) && !parseDateValue(cuota.fechaVencimiento))) {
      setLocalError('Complete las fechas de vencimiento con fechas válidas.');
      return;
    }

    const payload = {
      tieneAnticipo: Boolean(plan.tieneAnticipo),
      fechaAnticipo: plan.tieneAnticipo ? calendarDateToApi(plan.fechaAnticipo) : null,
      montoAnticipo: Number(plan.tieneAnticipo ? plan.montoAnticipo || 0 : 0),
      cantidadCuotas: Number(plan.cantidadCuotas || 0),
      fechaPrimerVencimiento: calendarDateToApi(plan.fechaPrimerVencimiento),
      periodicidad: plan.periodicidad,
      observaciones: plan.observaciones,
      cuotas: plan.cuotas.filter((cuota) => !isAnticipo(cuota)).map((cuota) => ({
        id: cuota.id,
        fechaVencimiento: calendarDateToApi(cuota.fechaVencimiento),
        importeOriginal: Number(cuota.importeOriginal || 0)
      }))
    };

    setLocalError('');
    onSave(payload);
  };

  if (!plan) {
    return <p className="empty-state">No hay plan de pago para editar.</p>;
  }

  return (
    <div>
      <div className="info-grid">
        <div>
          <strong>Periodicidad</strong>
          <p>{plan.periodicidad}</p>
        </div>
        <div>
          <strong>Cantidad de cuotas</strong>
          <p>{plan.cantidadCuotas}</p>
        </div>
        <div>
          <strong>Primer vencimiento</strong>
          <input
            type="date"
            value={plan.fechaPrimerVencimiento}
            onChange={(e) => handlePlanField('fechaPrimerVencimiento', e.target.value)}
            disabled={readOnly}
          />
        </div>
        <label className="via-toggle">
          <input
            type="checkbox"
            checked={Boolean(plan.tieneAnticipo)}
            onChange={(e) => handlePlanField('tieneAnticipo', e.target.checked)}
            disabled={readOnly}
          />
          Tiene anticipo
        </label>
        <div>
          <strong>Monto anticipo</strong>
          <input
            type="number"
            min="0"
            step="0.01"
            value={plan.montoAnticipo}
            onChange={(e) => handleAnticipoAmount(e.target.value)}
            disabled={readOnly || !plan.tieneAnticipo}
          />
        </div>
        {plan.tieneAnticipo && (
          <div>
            <label htmlFor="editorFechaAnticipo">Fecha de vencimiento del anticipo</label>
            <input
              id="editorFechaAnticipo"
              type="date"
              value={plan.fechaAnticipo || ''}
              onChange={(e) => handlePlanField('fechaAnticipo', e.target.value)}
              required
              disabled={readOnly}
            />
          </div>
        )}
        <div>
          <strong>Total cuotas</strong>
          <p>{money(totalCuotas, monedaCodigo)}</p>
        </div>
        <div>
          <strong>Total del plan</strong>
          <p>{money(totalPlan, monedaCodigo)}</p>
        </div>
        <div>
          <strong>Monto de la via</strong>
          <p>{money(viaMonto, monedaCodigo)}</p>
        </div>
      </div>

      {!totalsMatch && (
        <p className="form-error">El total del plan no coincide con el monto de la via. Diferencia: {money(difference, monedaCodigo)}</p>
      )}

      <div className="table-wrapper editable-plan-table">
        <table className="data-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Tipo</th>
              <th>Vencimiento</th>
              <th>Importe original</th>
              <th>Pagado</th>
              <th>Saldo</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {plan.cuotas.map((cuota) => (
              <tr key={cuota.id}>
                <td>{cuota.numeroCuota}</td>
                <td>{cuota.tipoCuota}</td>
                <td>
                  <input
                    type="date"
                    className="table-input"
                    value={isAnticipo(cuota) ? (plan.tieneAnticipo ? plan.fechaAnticipo || '' : cuota.fechaVencimiento) : cuota.fechaVencimiento}
                    onChange={(e) => isAnticipo(cuota)
                      ? handlePlanField('fechaAnticipo', e.target.value)
                      : handleCuotaField(cuota.id, 'fechaVencimiento', e.target.value)}
                    disabled={readOnly || (isAnticipo(cuota) && !plan.tieneAnticipo)}
                  />
                </td>
                <td>
                  <input
                    type="number"
                    className="table-input"
                    min="0"
                    step="0.01"
                    value={cuota.importeOriginal}
                    onChange={(e) => {
                      handleCuotaField(cuota.id, 'importeOriginal', e.target.value);
                      if (isAnticipo(cuota)) {
                        handlePlanField('montoAnticipo', e.target.value);
                      }
                    }}
                    disabled={readOnly || isAnticipo(cuota)}
                  />
                </td>
                <td>{money(cuota.importePagado, monedaCodigo)}</td>
                <td>{money(cuota.saldoPendiente, monedaCodigo)}</td>
                <td>{cuota.estado}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {(error || localError) && <p className="form-error">{localError || error}</p>}

      {readOnly ? (
        <p className="empty-state">El plan base queda fijo al aprobar el acuerdo. Los cambios posteriores se cargan como cuotas adicionales o de ajuste.</p>
      ) : (
        <div className="form-footer">
          <button className="btn-primary" type="button" onClick={handleSave} disabled={loading || !totalsMatch}>
            {loading ? 'Guardando cambios...' : 'Guardar personalizacion'}
          </button>
        </div>
      )}
    </div>
  );
};

export default PlanPagoEditor;

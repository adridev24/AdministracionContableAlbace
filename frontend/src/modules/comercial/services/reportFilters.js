import { calendarDateValue, formatCalendarDate } from '../../../shared/utils/calendarDate.js';

export const defaultReportFilters = () => ({ alcance: 'General', desde: '', hasta: '', via: 'Todos' });
export const generalReportFilters = (filters) => ({ ...defaultReportFilters(), via: filters.via });

export function reportQuery(filters = defaultReportFilters()) {
  const params = new URLSearchParams();
  if (filters.alcance === 'Periodo') {
    const desde = calendarDateValue(filters.desde);
    const hasta = calendarDateValue(filters.hasta);
    if (!desde || !hasta) throw new Error('Debe indicar ambas fechas válidas del período.');
    if (hasta < desde) throw new Error('La fecha hasta no puede ser anterior a la fecha desde.');
    params.set('desde', desde);
    params.set('hasta', hasta);
  }
  if (filters.via && filters.via !== 'Todos') params.set('via', filters.via);
  return params.toString();
}

// Snapshot only after a successful response; draft edits never relabel existing results.
export const appliedReportFilters = (request, response) => ({
  alcance: response.alcance,
  desde: calendarDateValue(response.periodoDesde),
  hasta: calendarDateValue(response.periodoHasta),
  via: request.via,
});

export function reportCaption(applied) {
  if (!applied) return '';
  const scope = applied.alcance === 'Periodo'
    ? `Período: ${formatCalendarDate(applied.desde)} al ${formatCalendarDate(applied.hasta)}`
    : 'Totales generales';
  const via = applied.via === 'Via1' ? 'Vía 1' : applied.via === 'Via2' ? 'Vía 2' : 'Todas las vías';
  return `${scope} · ${via} · Importes separados por moneda`;
}

export function estadoAcuerdosAbiertos(hasSelection, key, result) {
  if (!hasSelection) return { rows: [], loading: false, error: '', bloqueado: false, mensaje: '' };
  if (result.key !== key) return { rows: [], loading: true, error: '', bloqueado: true, mensaje: 'Esperá la consulta de acuerdos abiertos antes de guardar.' };
  const faltantes = result.rows.filter(a => !a.hitos.length);
  const mensaje = result.error || (faltantes.length ? 'Debe crear al menos un hito en cada acuerdo abierto de Vía 1 antes de guardar la factura: ' + faltantes.map(a => a.numeroAcuerdo).join(', ') + '.' : '');
  return { rows: result.rows, loading: false, error: result.error, bloqueado: Boolean(mensaje), mensaje };
}

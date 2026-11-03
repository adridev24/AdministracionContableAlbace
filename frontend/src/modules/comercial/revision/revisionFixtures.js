// Synthetic data shared by the isolated frontend tests; never used by application routes.
export const preparationFixture = () => ({
  acuerdoComercialId: 10, acuerdoComercialViaId: 20, planPagoId: 30, estadoAcuerdo: 'Aprobado', estadoVia: 'Aprobado',
  tieneAnticipo: true, montoAnticipo: 100, cantidadCuotas: 5, fechaPrimerVencimiento: '2026-11-01T00:00:00Z', periodicidad: 'Mensual',
  monedaCodigo: 'ARS', montoOriginal: 600, montoActual: 600, totalVigente: 600, totalPagadoValido: 80, saldoPendiente: 520, saldoAFavor: 0,
  versionVia: 201, versionPlan: 202, ultimaCuotaOrdinariaActivaId: 5, siguienteNumeroCuota: 9,
  cuotas: Array.from({ length: 6 }, (_, numero) => ({ id: numero, numero, tipo: numero ? 'Cuota' : 'Anticipo', importe: 100,
    vencimiento: '2026-11-01T00:00:00Z', estado: 'Pendiente', version: 300 + numero, tieneHistoria: numero === 4,
    puedeModificar: true, puedeRetirar: numero === 5 || numero === 0, motivoBloqueoModificacion: null,
    motivoBloqueoRetiro: numero > 0 && numero < 5 ? 'Solo puede retirarse la última cuota ordinaria activa.' : null }))
});

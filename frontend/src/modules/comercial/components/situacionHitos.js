const money = value => Math.round((Number(value || 0) + Number.EPSILON) * 100) / 100;
export function situacionHitos(via) {
  const hitos = (via.hitos || []).map(h => {
    const previsto = money(h.importeEstimado), facturado = money(h.importeFacturado), cobrado = money(h.importeAplicado);
    return { ...h, previsto, facturado, cobrado,
      pendienteFacturar: money(Math.max(0, previsto - facturado)),
      pendienteCobro: money(Math.max(0, facturado - cobrado)),
      estadoFacturacion: facturado <= 0 ? 'Sin facturar' : facturado < previsto ? 'Parcial' : 'Facturado',
      estadoCobranza: cobrado <= 0 ? 'Sin cobrar' : cobrado < facturado ? 'Parcial' : 'Cobrado' };
  });
  const sum = field => money(hitos.reduce((s, h) => s + Number(h[field] || 0), 0));
  const facturado = sum('facturado'), cobrado = sum('cobrado');
  return { hitos, acordado: money(via.montoActual), facturado, cobrado, reservado: sum('importeReservado'),
    pendienteFacturar: money(Math.max(0, via.montoActual - facturado)),
    pendienteCobro: money(Math.max(0, facturado - cobrado)),
    pendienteTotal: money(Math.max(0, via.montoActual - cobrado)) };
}

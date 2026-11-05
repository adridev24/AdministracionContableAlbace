export default function AlicuotaIibbSelect({ regimenes, fecha, value, onChange, disabled }) {
  const vigentes = regimenes.filter(r => r.vigenciaDesde.slice(0, 10) <= fecha &&
    (!r.vigenciaHasta || r.vigenciaHasta.slice(0, 10) >= fecha) && r.tipoBaseCalculo !== 'OtraBaseConfigurable');
  return <label className="form-field">Alícuota de percepción IIBB · Entre Ríos
    <select value={value} onChange={e => onChange(e.target.value)} disabled={disabled}>
      <option value="">Sin percepción</option>
      {vigentes.map(r => <option key={r.id} value={r.id}>{r.porcentaje}% · {r.descripcion} ({r.codigo})</option>)}
    </select>
  </label>;
}

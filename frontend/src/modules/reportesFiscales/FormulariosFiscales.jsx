import { useState } from 'react';

const condicionesIva = ['Responsable inscripto', 'Exento', 'Monotributista', 'Consumidor final', 'Sujeto del exterior'];

export function Campo({ titulo, nombre, datos, cambiar, tipo = 'text', opciones, required = true, ...props }) {
  const id = `fiscal-${nombre}`;
  return <label className="fiscal-campo" htmlFor={id}><span>{titulo}</span>{opciones
    ? <select id={id} value={datos[nombre] ?? ''} onChange={e => cambiar(nombre, e.target.value)} required={required} {...props}>
      {!opciones.some(o => (o.value ?? o) === '') && <option value="">Seleccionar…</option>}{opciones.map(o => <option key={o.value ?? o} value={o.value ?? o}>{o.label ?? o}</option>)}
    </select>
    : <input id={id} type={tipo} value={datos[nombre] ?? ''} onChange={e => cambiar(nombre, e.target.value)} required={required} {...props} />}</label>;
}

export function EmpresaForm({ empresa, guardar, busy }) {
  const [datos, setDatos] = useState(() => ({ razonSocial: '', cuit: '', domicilioFiscal: '', condicionIva: 'Responsable inscripto', inscripcionIibb: '', actividad: '', ...empresa, inicioActividades: empresa?.inicioActividades?.slice(0, 10) || '' }));
  const cambiar = (nombre, value) => setDatos(d => ({ ...d, [nombre]: value }));
  return <form onSubmit={e => { e.preventDefault(); guardar({ ...datos, inicioActividades: datos.inicioActividades || null }); }}>
    <h2>Datos fiscales de la empresa</h2>
    <p>Se utilizan en los reportes, la validación y el respaldo de las exportaciones.</p>
    <fieldset disabled={busy} className="fiscal-form-grid">
      <Campo titulo="Razón social" nombre="razonSocial" datos={datos} cambiar={cambiar} maxLength={200} />
      <Campo titulo="CUIT (11 dígitos)" nombre="cuit" datos={datos} cambiar={cambiar} inputMode="numeric" maxLength={13} />
      <Campo titulo="Domicilio fiscal" nombre="domicilioFiscal" datos={datos} cambiar={cambiar} maxLength={200} />
      <Campo titulo="Condición de IVA" nombre="condicionIva" datos={datos} cambiar={cambiar} opciones={condicionesIva.slice(0, 3)} />
      <Campo titulo="Inscripción en IIBB" nombre="inscripcionIibb" datos={datos} cambiar={cambiar} required={false} maxLength={40} />
      <Campo titulo="Actividad" nombre="actividad" datos={datos} cambiar={cambiar} required={false} maxLength={200} />
      <Campo titulo="Inicio de actividades" nombre="inicioActividades" tipo="date" datos={datos} cambiar={cambiar} required={false} />
    </fieldset>
    <button className="btn-primary" disabled={busy}>Guardar empresa</button>
    {empresa?.actualizadoEl && <p className="hint-text">Última actualización: {new Date(empresa.actualizadoEl).toLocaleString('es-AR')} · {empresa.actualizadoPor}</p>}
  </form>;
}

export function VentaFiscalForm({ venta, tipos, guardar, cancelar, busy }) {
  const [datos, setDatos] = useState(() => ({ ...venta.fiscal }));
  const cambiar = (nombre, value) => setDatos(d => ({ ...d, [nombre]: value }));
  return <form className="fiscal-editor" onSubmit={e => { e.preventDefault(); guardar(venta.id, { ...datos, tipoDocumento: Number(datos.tipoDocumento), tipoComprobanteArca: Number(datos.tipoComprobanteArca) }); }}>
    <h3>Datos fiscales · {venta.tipo} {venta.puntoVenta}-{venta.numero}</h3>
    <p>Confirme los datos del comprobante original. Los importes se toman de la venta.</p>
    <fieldset disabled={busy} className="fiscal-form-grid">
      <Campo titulo="Tipo de comprobante ARCA" nombre="tipoComprobanteArca" datos={datos} cambiar={cambiar} opciones={tipos.map(t => ({ value: t.codigo, label: `${String(t.codigo).padStart(3, '0')} · ${t.descripcion}` }))} />
      <Campo titulo="Tipo de documento del comprador" nombre="tipoDocumento" datos={datos} cambiar={cambiar} opciones={[{ value: 80, label: 'CUIT' }, { value: 86, label: 'CUIL' }, { value: 96, label: 'DNI' }]} />
      <Campo titulo="Número de documento" nombre="documento" datos={datos} cambiar={cambiar} maxLength={20} />
      <Campo titulo="Razón social del comprador" nombre="razonSocial" datos={datos} cambiar={cambiar} maxLength={200} />
      <Campo titulo="Condición de IVA del comprador" nombre="condicionIva" datos={datos} cambiar={cambiar} opciones={condicionesIva} />
      <Campo titulo="Código de operación" nombre="codigoOperacion" datos={datos} cambiar={cambiar} required={false}
        opciones={[{ value: '', label: 'Gravada / no corresponde' }, { value: 'E', label: 'Exenta' }, { value: 'N', label: 'No gravada' }, { value: 'A', label: 'No alcanzada' }, { value: 'X', label: 'Exportación al exterior' }, { value: 'Z', label: 'Exportación a zona franca' }]} />
      {venta.devolucionCheques !== 0 && <label className="fiscal-checkbox"><input type="checkbox" checked={datos.devolucionChequeRevisada} onChange={e => cambiar('devolucionChequeRevisada', e.target.checked)} />Revisé la devolución de cheques y confirmo su inclusión en el importe no gravado del comprobante.</label>}
    </fieldset>
    <div className="fiscal-actions"><button disabled={busy} className="btn-primary">Confirmar datos fiscales</button><button disabled={busy} type="button" className="btn-secondary" onClick={cancelar}>Cancelar</button></div>
  </form>;
}

export function RetencionFiscalForm({ retencion, periodo, guardar, cancelar, busy }) {
  const [datos, setDatos] = useState(() => ({ cuitAgente: '', razonSocialAgente: '', certificado: '', regimen: '', jurisdiccion: '', periodoComputo: periodo, ...retencion.fiscal, fechaRetencion: (retencion.fiscal?.fechaRetencion || retencion.fechaCobranza).slice(0, 10) }));
  const cambiar = (nombre, value) => setDatos(d => ({ ...d, [nombre]: value }));
  return <form className="fiscal-editor" onSubmit={e => { e.preventDefault(); guardar(retencion.id, datos); }}>
    <h3>Certificado de {retencion.impuesto} · Cobranza #{retencion.cobranzaId}</h3>
    <p>El importe de {retencion.importe.toLocaleString('es-AR')} {retencion.moneda} proviene de la cobranza confirmada.</p>
    <fieldset disabled={busy} className="fiscal-form-grid">
      <Campo titulo="CUIT del agente" nombre="cuitAgente" datos={datos} cambiar={cambiar} maxLength={13} />
      <Campo titulo="Razón social del agente" nombre="razonSocialAgente" datos={datos} cambiar={cambiar} maxLength={200} />
      <Campo titulo="Número de certificado" nombre="certificado" datos={datos} cambiar={cambiar} maxLength={50} />
      <Campo titulo="Código de régimen" nombre="regimen" datos={datos} cambiar={cambiar} maxLength={20} />
      <Campo titulo="Jurisdicción" nombre="jurisdiccion" datos={datos} cambiar={cambiar} required={retencion.impuesto === 'IIBB'} maxLength={50} />
      <Campo titulo="Fecha del certificado" nombre="fechaRetencion" tipo="date" datos={datos} cambiar={cambiar} />
      <Campo titulo="Período de cómputo" nombre="periodoComputo" tipo="month" datos={datos} cambiar={cambiar} />
    </fieldset>
    <div className="fiscal-actions"><button disabled={busy} className="btn-primary">Guardar certificado</button><button disabled={busy} type="button" className="btn-secondary" onClick={cancelar}>Cancelar</button></div>
  </form>;
}

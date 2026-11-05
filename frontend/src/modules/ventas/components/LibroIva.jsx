import { Link } from 'react-router-dom';
import { useState } from 'react';
import ventasService from '../services/ventasService';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import '../ventas.css';

const columns = {
  neto: 'Neto',
  iva: 'IVA débito',
  noGravado: 'Otros no gravados',
  exento: 'Exento',
  iibb: 'Perc. IIBB',
  devolucion: 'Devolución cheques',
  total: 'Total',
};

export default function LibroIva() {
  const [periodo, setPeriodo] = useState({ desde: '', hasta: '' });
  const [rows, setRows] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function consultar(event) {
    event.preventDefault();
    setBusy(true);
    setRows(null);
    setError('');
    try {
      setRows(await ventasService.libroIva(periodo));
    } catch (err) {
      setError(err.response?.data?.error || 'No se pudo consultar el libro.');
    } finally {
      setBusy(false);
    }
  }

  function cambiarFecha(event) {
    const { name, value } = event.target;
    setPeriodo(prev => ({ ...prev, [name]: value }));
    setRows(null);
    setError('');
  }

  function limpiar() {
    setPeriodo({ desde: '', hasta: '' });
    setRows(null);
    setError('');
  }

  return (
    <div className="page-container ventas-page">
      <div className="page-header">
        <div>
          <h1>Libro IVA Ventas</h1>
          <p className="page-subtitle">Consulta de comprobantes e importes fiscales por período.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
          <Link className="btn-secondary" to="/ventas/cuenta-corriente">Cuenta corriente</Link>
          <Link className="btn-secondary" to="/ventas/cobranzas">Cobranzas</Link>
          <Link className="btn-secondary" to="/">Principal</Link>
        </div>
      </div>

      <SectionCard
        title="Buscar en Libro IVA Ventas"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={limpiar} disabled={busy}>Limpiar</button>
            <button className="btn-primary" type="submit" form="libro-iva-filtros" disabled={busy}>
              {busy ? 'Consultando...' : 'Consultar'}
            </button>
          </>
        )}
      >
        <form id="libro-iva-filtros" onSubmit={consultar}>
          <div className="form-grid cuenta-corriente-filter-grid">
            <div className="form-field">
              <label htmlFor="libro-iva-desde">Fecha desde</label>
              <input id="libro-iva-desde" name="desde" type="date" required disabled={busy}
                value={periodo.desde} max={periodo.hasta || undefined} onChange={cambiarFecha} />
            </div>
            <div className="form-field">
              <label htmlFor="libro-iva-hasta">Fecha hasta</label>
              <input id="libro-iva-hasta" name="hasta" type="date" required disabled={busy}
                value={periodo.hasta} min={periodo.desde || undefined} onChange={cambiarFecha} />
            </div>
          </div>
          <p className="hint-text">El período incluye la fecha desde y la fecha hasta.</p>
        </form>
      </SectionCard>

      {error && <p className="form-error" role="alert">{error}</p>}
      {busy && <LoadingSpinner />}
      {rows && (
        <SectionCard title="Resultados" description={rows.length + ' comprobante(s).'}>
          {rows.length ? (
            <div className="responsive-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    {['Fecha', 'Comprobante', 'Cliente', 'Moneda', ...Object.values(columns)].map(h => <th key={h}>{h}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row, index) => (
                    <tr key={index}>
                      <td>{row.fechaComprobante.slice(0, 10)}</td>
                      <td>{String(row.puntoVenta).padStart(5, '0')}-{String(row.numeroComprobante).padStart(8, '0')}</td>
                      <td>{row.clienteExternoId}</td>
                      <td>{row.monedaCodigo}</td>
                      {Object.keys(columns).map(key => <td key={key}>{row[key].toFixed(2)}</td>)}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : <p className="empty-state">Sin comprobantes registrados en el período.</p>}
        </SectionCard>
      )}
    </div>
  );
}

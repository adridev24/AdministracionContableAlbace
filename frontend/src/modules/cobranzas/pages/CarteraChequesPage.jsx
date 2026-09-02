import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import externalDataService from '../../comercial/services/externalDataService';
import cobranzasService from '../services/cobranzasService';
import carteraChequesService from '../services/carteraChequesService';
import '../../ventas/ventas.css';

const today = new Date().toISOString().slice(0, 10);

const initialFilters = {
  estado: '',
  moneda: '',
  bancoId: '',
  clienteId: '',
  fechaVencimientoDesde: '',
  fechaVencimientoHasta: '',
};

const initialDeposito = {
  fechaDeposito: today,
  bancoDestino: '',
  cuentaDestino: '',
};

const money = (value) => Number(value || 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatDate = (value) => (value ? String(value).slice(0, 10) : '-');
const getErrorMessage = (error) => (
  error?.response?.data?.error ||
  error?.response?.data?.detail ||
  error?.response?.data?.title ||
  'No se pudo completar la operacion.'
);

const CarteraChequesPage = () => {
  const [filters, setFilters] = useState(initialFilters);
  const [cheques, setCheques] = useState([]);
  const [selectedCheque, setSelectedCheque] = useState(null);
  const [clientes, setClientes] = useState([]);
  const [bancos, setBancos] = useState([]);
  const [depositoForm, setDepositoForm] = useState(initialDeposito);
  const [acreditacionFecha, setAcreditacionFecha] = useState(today);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadCheques = async (nextFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const data = await carteraChequesService.getCheques(nextFilters);
      setCheques(data || []);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let mounted = true;
    Promise.all([
      externalDataService.getClients(),
      cobranzasService.getBancos(true),
      carteraChequesService.getCheques(initialFilters),
    ])
      .then(([clientesData, bancosData, chequesData]) => {
        if (!mounted) return;
        setClientes(clientesData || []);
        setBancos(bancosData || []);
        setCheques(chequesData || []);
      })
      .catch((loadError) => setError(getErrorMessage(loadError)))
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, []);

  const resumen = useMemo(() => {
    const grouped = {};
    cheques.forEach((cheque) => {
      const key = `${cheque.estado}|${cheque.monedaCodigo}`;
      grouped[key] = grouped[key] || { estado: cheque.estado, monedaCodigo: cheque.monedaCodigo, total: 0 };
      grouped[key].total += Number(cheque.importe || 0);
    });
    return Object.values(grouped).sort((a, b) => `${a.estado}${a.monedaCodigo}`.localeCompare(`${b.estado}${b.monedaCodigo}`));
  }, [cheques]);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleSearch = () => {
    loadCheques(filters);
  };

  const handleClear = () => {
    setFilters(initialFilters);
    loadCheques(initialFilters);
  };

  const handleSelect = async (id) => {
    setError('');
    setSuccess('');
    setSelectedCheque(await carteraChequesService.getCheque(id));
  };

  const handleDepositoChange = (event) => {
    const { name, value } = event.target;
    setDepositoForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleDepositar = async (event) => {
    event.preventDefault();
    if (!selectedCheque) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const updated = await carteraChequesService.depositar(selectedCheque.id, depositoForm);
      setSelectedCheque(updated);
      setDepositoForm(initialDeposito);
      await loadCheques(filters);
      setSuccess('Cheque depositado.');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handleAcreditar = async (event) => {
    event.preventDefault();
    if (!selectedCheque) return;
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const updated = await carteraChequesService.acreditar(selectedCheque.id, { fechaAcreditacion: acreditacionFecha });
      setSelectedCheque(updated);
      await loadCheques(filters);
      setSuccess('Cheque acreditado.');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page-container ventas-page cobranzas-page">
      <div className="page-header">
        <div>
          <h1>Cartera de cheques</h1>
          <p className="page-subtitle">Cheques de terceros ingresados por cobranzas confirmadas.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/cobranzas">Cobranzas</Link>
          <Link className="btn-secondary" to="/ventas">Ventas</Link>
        </div>
      </div>

      <SectionCard
        title="Filtros"
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={handleClear} disabled={loading}>Limpiar</button>
            <button className="btn-primary" type="button" onClick={handleSearch} disabled={loading}>{loading ? 'Buscando...' : 'Buscar'}</button>
          </>
        )}
      >
        <div className="form-grid">
          <div className="form-field">
            <label>Estado</label>
            <select name="estado" value={filters.estado} onChange={handleFilterChange}>
              <option value="">Todos</option>
              <option value="EN_CARTERA">EN_CARTERA</option>
              <option value="DEPOSITADO">DEPOSITADO</option>
              <option value="ACREDITADO">ACREDITADO</option>
              <option value="RECHAZADO">RECHAZADO</option>
            </select>
          </div>
          <div className="form-field">
            <label>Moneda</label>
            <select name="moneda" value={filters.moneda} onChange={handleFilterChange}>
              <option value="">Todas</option>
              <option value="ARS">ARS</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="form-field">
            <label>Banco</label>
            <select name="bancoId" value={filters.bancoId} onChange={handleFilterChange}>
              <option value="">Todos</option>
              {bancos.map((banco) => <option key={banco.id} value={banco.id}>{banco.nombre}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Cliente</label>
            <select name="clienteId" value={filters.clienteId} onChange={handleFilterChange}>
              <option value="">Todos</option>
              {clientes.map((cliente) => <option key={cliente.idCliente} value={String(cliente.idCliente)}>{cliente.nombreCliente}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Vencimiento desde</label>
            <input name="fechaVencimientoDesde" type="date" value={filters.fechaVencimientoDesde} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Vencimiento hasta</label>
            <input name="fechaVencimientoHasta" type="date" value={filters.fechaVencimientoHasta} onChange={handleFilterChange} />
          </div>
        </div>
      </SectionCard>

      <SectionCard title="Resumen por estado y moneda">
        <div className="summary-grid">
          {resumen.length === 0 && <div><span>Sin cheques</span><strong>0,00</strong></div>}
          {resumen.map((item) => (
            <div key={`${item.estado}-${item.monedaCodigo}`}>
              <span>{item.estado}</span>
              <strong>{item.monedaCodigo} {money(item.total)}</strong>
            </div>
          ))}
        </div>
      </SectionCard>

      <SectionCard title="Cheques">
        {loading ? <LoadingSpinner /> : (
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Numero</th>
                  <th>Banco</th>
                  <th>Librador</th>
                  <th>Cliente origen</th>
                  <th>Vencimiento</th>
                  <th>Importe</th>
                  <th>Moneda</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {cheques.map((cheque) => (
                  <tr key={cheque.id}>
                    <td>{cheque.numeroCheque}</td>
                    <td>{cheque.banco}</td>
                    <td>{cheque.librador}</td>
                    <td>{cheque.clienteExternoId}</td>
                    <td>{formatDate(cheque.fechaVencimiento)}</td>
                    <td>{money(cheque.importe)}</td>
                    <td>{cheque.monedaCodigo}</td>
                    <td><span className="status-pill is-active">{cheque.estado}</span></td>
                    <td><button className="btn-secondary" type="button" onClick={() => handleSelect(cheque.id)}>Ver</button></td>
                  </tr>
                ))}
                {cheques.length === 0 && (
                  <tr><td colSpan="9" className="empty-cell">No hay cheques para los filtros seleccionados.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      {selectedCheque && (
        <SectionCard title={`Cheque ${selectedCheque.numeroCheque}`}>
          <div className="summary-grid">
            <div><span>Banco</span><strong>{selectedCheque.banco}</strong></div>
            <div><span>Emision</span><strong>{formatDate(selectedCheque.fechaEmision)}</strong></div>
            <div><span>Vencimiento</span><strong>{formatDate(selectedCheque.fechaVencimiento)}</strong></div>
            <div><span>Importe</span><strong>{selectedCheque.monedaCodigo} {money(selectedCheque.importe)}</strong></div>
            <div><span>Librador</span><strong>{selectedCheque.librador}</strong></div>
            <div><span>CUIT</span><strong>{selectedCheque.cuitLibrador}</strong></div>
            <div><span>Estado</span><strong>{selectedCheque.estado}</strong></div>
            <div><span>Cliente origen</span><strong>{selectedCheque.clienteExternoId}</strong></div>
            <div><span>Cobranza origen</span><strong>{selectedCheque.cobranzaId}</strong></div>
            <div><span>Medio origen</span><strong>{selectedCheque.medioPagoDescripcion}</strong></div>
            <div><span>Ingreso</span><strong>{formatDate(selectedCheque.fechaAlta)}</strong></div>
            <div><span>Usuario ingreso</span><strong>{selectedCheque.usuarioAlta}</strong></div>
          </div>
          {selectedCheque.observaciones && <p className="form-warning">{selectedCheque.observaciones}</p>}

          {selectedCheque.estado === 'EN_CARTERA' && (
            <form className="venta-form" onSubmit={handleDepositar}>
              <div className="form-grid">
                <div className="form-field">
                  <label>Fecha deposito</label>
                  <input name="fechaDeposito" type="date" value={depositoForm.fechaDeposito} onChange={handleDepositoChange} required />
                </div>
                <div className="form-field">
                  <label>Banco destino</label>
                  <input name="bancoDestino" value={depositoForm.bancoDestino} onChange={handleDepositoChange} required />
                </div>
                <div className="form-field">
                  <label>Cuenta destino</label>
                  <input name="cuentaDestino" value={depositoForm.cuentaDestino} onChange={handleDepositoChange} required />
                </div>
              </div>
              <div className="form-actions">
                <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Depositando...' : 'Depositar'}</button>
              </div>
            </form>
          )}

          {selectedCheque.estado === 'DEPOSITADO' && (
            <>
              <div className="summary-grid">
                <div><span>Fecha deposito</span><strong>{formatDate(selectedCheque.fechaDeposito)}</strong></div>
                <div><span>Banco destino</span><strong>{selectedCheque.bancoDestino || '-'}</strong></div>
                <div><span>Cuenta destino</span><strong>{selectedCheque.cuentaDestino || '-'}</strong></div>
                <div><span>Usuario deposito</span><strong>{selectedCheque.usuarioDeposito || '-'}</strong></div>
              </div>
              <form className="venta-form" onSubmit={handleAcreditar}>
                <div className="form-grid">
                  <div className="form-field">
                    <label>Fecha acreditacion</label>
                    <input type="date" value={acreditacionFecha} onChange={(event) => setAcreditacionFecha(event.target.value)} required />
                  </div>
                </div>
                <div className="form-actions">
                  <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Acreditando...' : 'Acreditar'}</button>
                </div>
              </form>
            </>
          )}

          {selectedCheque.estado === 'ACREDITADO' && (
            <div className="summary-grid">
              <div><span>Fecha deposito</span><strong>{formatDate(selectedCheque.fechaDeposito)}</strong></div>
              <div><span>Banco destino</span><strong>{selectedCheque.bancoDestino || '-'}</strong></div>
              <div><span>Cuenta destino</span><strong>{selectedCheque.cuentaDestino || '-'}</strong></div>
              <div><span>Fecha acreditacion</span><strong>{formatDate(selectedCheque.fechaAcreditacion)}</strong></div>
              <div><span>Usuario acreditacion</span><strong>{selectedCheque.usuarioAcreditacion || '-'}</strong></div>
            </div>
          )}
        </SectionCard>
      )}

      {error && <p className="form-error">{error}</p>}
      {success && <p className="form-success">{success}</p>}
    </div>
  );
};

export default CarteraChequesPage;

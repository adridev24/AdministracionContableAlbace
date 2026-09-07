import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import SectionCard from '../../../shared/components/SectionCard';
import cuentasContablesService from '../../contabilidad/services/cuentasContablesService';
import tesoreriaService from '../services/tesoreriaService';
import '../../ventas/ventas.css';

const emptyBancoForm = { codigo: '', nombre: '', activo: true };
const emptyCuentaForm = {
  bancoId: '',
  descripcion: '',
  tipoCuenta: 'CUENTA_CORRIENTE',
  monedaCodigo: 'ARS',
  numeroCuenta: '',
  cbu: '',
  aliasCbu: '',
  cuentaContableId: '',
  activa: true,
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';
const formatCuenta = (cuenta) => `${cuenta.bancoNombre} - ${cuenta.descripcion} - ${cuenta.numeroCuenta} - ${cuenta.monedaCodigo}`;

const TesoreriaPage = () => {
  const [bancos, setBancos] = useState([]);
  const [cuentasBancarias, setCuentasBancarias] = useState([]);
  const [cuentasContables, setCuentasContables] = useState([]);
  const [bancoForm, setBancoForm] = useState(emptyBancoForm);
  const [cuentaForm, setCuentaForm] = useState(emptyCuentaForm);
  const [selectedBanco, setSelectedBanco] = useState(null);
  const [selectedCuenta, setSelectedCuenta] = useState(null);
  const [bancoSearch, setBancoSearch] = useState('');
  const [cuentaSearch, setCuentaSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const cuentasContablesActivas = useMemo(
    () => cuentasContables.filter((cuenta) => cuenta.activa),
    [cuentasContables],
  );

  const filteredBancos = useMemo(() => {
    const term = bancoSearch.trim().toLowerCase();
    return bancos.filter((banco) => !term || banco.codigo.toLowerCase().includes(term) || banco.nombre.toLowerCase().includes(term));
  }, [bancos, bancoSearch]);

  const filteredCuentas = useMemo(() => {
    const term = cuentaSearch.trim().toLowerCase();
    return cuentasBancarias.filter((cuenta) => (
      !term ||
      cuenta.bancoNombre.toLowerCase().includes(term) ||
      cuenta.descripcion.toLowerCase().includes(term) ||
      cuenta.numeroCuenta.toLowerCase().includes(term) ||
      cuenta.monedaCodigo.toLowerCase().includes(term)
    ));
  }, [cuentasBancarias, cuentaSearch]);

  const loadData = async () => {
    setLoading(true);
    setError('');
    try {
      const [bancosData, cuentasData, cuentasContablesData] = await Promise.all([
        tesoreriaService.getBancos(),
        tesoreriaService.getCuentasBancarias(),
        cuentasContablesService.getCuentas({ activa: true }),
      ]);
      setBancos(bancosData || []);
      setCuentasBancarias(cuentasData || []);
      setCuentasContables(cuentasContablesData || []);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleBancoChange = (event) => {
    const { checked, name, type, value } = event.target;
    setBancoForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleCuentaChange = (event) => {
    const { checked, name, type, value } = event.target;
    setCuentaForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const resetBancoForm = () => {
    setSelectedBanco(null);
    setBancoForm(emptyBancoForm);
  };

  const resetCuentaForm = () => {
    setSelectedCuenta(null);
    setCuentaForm(emptyCuentaForm);
  };

  const handleEditBanco = (banco) => {
    setSelectedBanco(banco);
    setBancoForm({ codigo: banco.codigo, nombre: banco.nombre, activo: Boolean(banco.activo) });
    setMessage('');
    setError('');
  };

  const handleEditCuenta = (cuenta) => {
    setSelectedCuenta(cuenta);
    setCuentaForm({
      bancoId: String(cuenta.bancoId),
      descripcion: cuenta.descripcion || '',
      tipoCuenta: cuenta.tipoCuenta || 'CUENTA_CORRIENTE',
      monedaCodigo: cuenta.monedaCodigo || 'ARS',
      numeroCuenta: cuenta.numeroCuenta || '',
      cbu: cuenta.cbu || '',
      aliasCbu: cuenta.aliasCbu || '',
      cuentaContableId: String(cuenta.cuentaContableId),
      activa: Boolean(cuenta.activa),
    });
    setMessage('');
    setError('');
  };

  const saveBanco = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = {
        codigo: bancoForm.codigo.trim(),
        nombre: bancoForm.nombre.trim(),
        activo: bancoForm.activo,
      };
      if (selectedBanco?.id) {
        await tesoreriaService.updateBanco(selectedBanco.id, payload);
        setMessage('Banco actualizado.');
      } else {
        await tesoreriaService.createBanco(payload);
        setMessage('Banco creado.');
      }
      resetBancoForm();
      await loadData();
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const saveCuenta = async (event) => {
    event.preventDefault();
    setSaving(true);
    setMessage('');
    setError('');
    try {
      const payload = {
        bancoId: Number(cuentaForm.bancoId),
        descripcion: cuentaForm.descripcion.trim(),
        tipoCuenta: cuentaForm.tipoCuenta,
        monedaCodigo: cuentaForm.monedaCodigo,
        numeroCuenta: cuentaForm.numeroCuenta.trim(),
        cbu: cuentaForm.cbu.trim() || null,
        aliasCbu: cuentaForm.aliasCbu.trim() || null,
        cuentaContableId: Number(cuentaForm.cuentaContableId),
        activa: cuentaForm.activa,
      };
      if (selectedCuenta?.id) {
        await tesoreriaService.updateCuentaBancaria(selectedCuenta.id, payload);
        setMessage('Cuenta bancaria actualizada.');
      } else {
        await tesoreriaService.createCuentaBancaria(payload);
        setMessage('Cuenta bancaria creada.');
      }
      resetCuentaForm();
      await loadData();
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="page-container ventas-page">
      <div className="page-header">
        <div>
          <h1>Tesoreria</h1>
          <p className="page-subtitle">Parametrizacion basica de bancos y cuentas bancarias propias.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/cartera-cheques">Cartera de cheques</Link>
          <Link className="btn-secondary" to="/">Principal</Link>
        </div>
      </div>

      <SectionCard title={selectedBanco ? 'Editar banco' : 'Nuevo banco'}>
        <form className="venta-form" onSubmit={saveBanco}>
          <div className="form-grid">
            <div className="form-field">
              <label>Codigo</label>
              <input name="codigo" value={bancoForm.codigo} onChange={handleBancoChange} required />
            </div>
            <div className="form-field">
              <label>Banco</label>
              <input name="nombre" value={bancoForm.nombre} onChange={handleBancoChange} required />
            </div>
            <div className="form-field">
              <label>Estado</label>
              <label><input name="activo" type="checkbox" checked={bancoForm.activo} onChange={handleBancoChange} /> Activo</label>
            </div>
          </div>
          <div className="form-actions">
            {selectedBanco && <button className="btn-secondary" type="button" onClick={resetBancoForm} disabled={saving}>Cancelar</button>}
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar banco'}</button>
          </div>
        </form>
      </SectionCard>

      <SectionCard title="Bancos">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={bancoSearch} onChange={(event) => setBancoSearch(event.target.value)} placeholder="Codigo o banco" />
          </div>
        </div>
        {loading ? <LoadingSpinner /> : (
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Codigo</th>
                  <th>Banco</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredBancos.map((banco) => (
                  <tr key={banco.id}>
                    <td>{banco.codigo}</td>
                    <td>{banco.nombre}</td>
                    <td><span className={`status-pill ${banco.activo ? 'is-active' : 'is-inactive'}`}>{banco.activo ? 'Activo' : 'Inactivo'}</span></td>
                    <td><button className="btn-secondary" type="button" onClick={() => handleEditBanco(banco)} disabled={saving}>Editar</button></td>
                  </tr>
                ))}
                {!filteredBancos.length && <tr><td colSpan="4" className="empty-cell">No hay bancos para mostrar.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      <SectionCard title={selectedCuenta ? 'Editar cuenta bancaria' : 'Nueva cuenta bancaria'}>
        <form className="venta-form" onSubmit={saveCuenta}>
          <div className="form-grid">
            <div className="form-field">
              <label>Banco</label>
              <select name="bancoId" value={cuentaForm.bancoId} onChange={handleCuentaChange} required>
                <option value="">Seleccionar</option>
                {bancos.filter((banco) => banco.activo).map((banco) => (
                  <option key={banco.id} value={banco.id}>{banco.nombre}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Descripcion / Alias</label>
              <input name="descripcion" value={cuentaForm.descripcion} onChange={handleCuentaChange} required />
            </div>
            <div className="form-field">
              <label>Tipo de cuenta</label>
              <select name="tipoCuenta" value={cuentaForm.tipoCuenta} onChange={handleCuentaChange} required>
                <option value="CUENTA_CORRIENTE">Cuenta corriente</option>
                <option value="CAJA_AHORRO">Caja de ahorro</option>
                <option value="OTRA">Otra</option>
              </select>
            </div>
            <div className="form-field">
              <label>Moneda</label>
              <select name="monedaCodigo" value={cuentaForm.monedaCodigo} onChange={handleCuentaChange} required>
                <option value="ARS">ARS</option>
                <option value="USD">USD</option>
              </select>
            </div>
            <div className="form-field">
              <label>Numero de cuenta</label>
              <input name="numeroCuenta" value={cuentaForm.numeroCuenta} onChange={handleCuentaChange} required />
            </div>
            <div className="form-field">
              <label>CBU</label>
              <input name="cbu" value={cuentaForm.cbu} onChange={handleCuentaChange} />
            </div>
            <div className="form-field">
              <label>Alias CBU</label>
              <input name="aliasCbu" value={cuentaForm.aliasCbu} onChange={handleCuentaChange} />
            </div>
            <div className="form-field">
              <label>Cuenta contable</label>
              <select name="cuentaContableId" value={cuentaForm.cuentaContableId} onChange={handleCuentaChange} required>
                <option value="">Seleccionar</option>
                {cuentasContablesActivas.map((cuenta) => (
                  <option key={cuenta.id} value={cuenta.id}>{cuenta.codigo} - {cuenta.nombre}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Estado</label>
              <label><input name="activa" type="checkbox" checked={cuentaForm.activa} onChange={handleCuentaChange} /> Activa</label>
            </div>
          </div>
          <div className="form-actions">
            {selectedCuenta && <button className="btn-secondary" type="button" onClick={resetCuentaForm} disabled={saving}>Cancelar</button>}
            <button className="btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar cuenta'}</button>
          </div>
        </form>
      </SectionCard>

      <SectionCard title="Cuentas bancarias propias">
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Buscar</label>
            <input value={cuentaSearch} onChange={(event) => setCuentaSearch(event.target.value)} placeholder="Banco, alias, numero o moneda" />
          </div>
        </div>
        {loading ? <LoadingSpinner /> : (
          <div className="responsive-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Cuenta</th>
                  <th>Tipo</th>
                  <th>CBU</th>
                  <th>Cuenta contable</th>
                  <th>Estado</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filteredCuentas.map((cuenta) => (
                  <tr key={cuenta.id}>
                    <td>{formatCuenta(cuenta)}</td>
                    <td>{cuenta.tipoCuenta}</td>
                    <td>{cuenta.cbu || cuenta.aliasCbu || '-'}</td>
                    <td>{cuenta.cuentaContableCodigo} - {cuenta.cuentaContableNombre}</td>
                    <td><span className={`status-pill ${cuenta.activa ? 'is-active' : 'is-inactive'}`}>{cuenta.activa ? 'Activa' : 'Inactiva'}</span></td>
                    <td><button className="btn-secondary" type="button" onClick={() => handleEditCuenta(cuenta)} disabled={saving}>Editar</button></td>
                  </tr>
                ))}
                {!filteredCuentas.length && <tr><td colSpan="6" className="empty-cell">No hay cuentas bancarias para mostrar.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </SectionCard>

      {message && <p className="form-success">{message}</p>}
      {error && <p className="form-error">{error}</p>}
    </div>
  );
};

export default TesoreriaPage;

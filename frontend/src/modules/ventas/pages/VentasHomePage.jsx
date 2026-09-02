import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import ClienteObraSelector from '../../comercial/components/ClienteObraSelector';
import VentaHeaderForm from '../components/VentaHeaderForm';
import VentasTable from '../components/VentasTable';
import ventasService from '../services/ventasService';
import '../ventas.css';

const initialFilters = {
  fechaDesde: '',
  fechaHasta: '',
  clienteExternoId: '',
  obraExternaId: '',
  tipoComprobanteVentaId: '',
  puntoVenta: '',
  numeroComprobante: '',
  estado: 'Borrador',
  page: 1,
  pageSize: 50,
};

const getErrorMessage = (error) => error?.response?.data?.error || 'No se pudo completar la operacion.';

const VentasHomePage = () => {
  const [tiposComprobante, setTiposComprobante] = useState([]);
  const [filters, setFilters] = useState(initialFilters);
  const [ventas, setVentas] = useState([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [selectedVenta, setSelectedVenta] = useState(null);

  const loadVentas = async (nextFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const response = await ventasService.getVentas(nextFilters);
      setVentas(response.items || []);
      setTotal(response.total || 0);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    ventasService.getTiposComprobante(true)
      .then((data) => setTiposComprobante(data || []))
      .catch(() => setError('No se pudieron cargar los tipos de comprobante.'));
  }, []);

  useEffect(() => {
    let mounted = true;
    ventasService.getVentas(initialFilters)
      .then((response) => {
        if (!mounted) return;
        setVentas(response.items || []);
        setTotal(response.total || 0);
      })
      .catch((loadError) => {
        if (mounted) setError(getErrorMessage(loadError));
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });

    return () => { mounted = false; };
  }, []);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value, page: 1 }));
  };

  const handleClienteObraFilterChange = (values) => {
    setFilters((prev) => ({
      ...prev,
      clienteExternoId: values.clienteExternoId ?? prev.clienteExternoId,
      obraExternaId: values.obraExternoId ?? values.obraExternaId ?? '',
      page: 1,
    }));
  };

  const handleSearch = () => {
    loadVentas(filters);
  };

  const handleClearFilters = () => {
    setFilters(initialFilters);
    loadVentas(initialFilters);
  };

  const handleSubmit = async (payload) => {
    setSaving(true);
    setError('');
    try {
      if (selectedVenta?.id) {
        await ventasService.updateVenta(selectedVenta.id, payload);
      } else {
        await ventasService.createVenta(payload);
      }
      setSelectedVenta(null);
      await loadVentas(filters);
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
          <h1>Ventas</h1>
          <p className="page-subtitle">Encabezados de comprobantes en estado Borrador.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/ventas/parametrizacion">Parametrizacion</Link>
          <Link className="btn-secondary" to="/">Principal</Link>
        </div>
      </div>

      <SectionCard title="Menu de Ventas">
        <div className="module-menu-grid">
          <Link className="module-menu-item" to="/ventas">
            <strong>Operaciones</strong>
            <span>Alta y consulta de encabezados de facturas en borrador.</span>
          </Link>
          <Link className="module-menu-item" to="/ventas/parametrizacion">
            <strong>Parametrizacion</strong>
            <span>Comprobantes, puntos de venta y parametros tributarios.</span>
          </Link>
          <Link className="module-menu-item" to="/ventas/cobranzas">
            <strong>Cobranzas</strong>
            <span>Cancelacion efectiva de facturas confirmadas de Via 1.</span>
          </Link>
          <Link className="module-menu-item" to="/ventas/cuenta-corriente">
            <strong>Cuenta corriente</strong>
            <span>Saldos, movimientos y facturas por cliente y moneda.</span>
          </Link>
        </div>
      </SectionCard>

      <SectionCard
        title={selectedVenta ? 'Editar borrador' : 'Nueva venta en borrador'}
        description="Solo se registra el encabezado. Los detalles, totales e impacto contable quedan para etapas posteriores."
      >
        <VentaHeaderForm
          key={selectedVenta?.id || 'new'}
          tiposComprobante={tiposComprobante}
          selectedVenta={selectedVenta}
          saving={saving}
          onCancel={() => setSelectedVenta(null)}
          onSubmit={handleSubmit}
        />
      </SectionCard>

      <SectionCard
        title="Buscar ventas"
        description="Filtra encabezados de venta registrados en PostgreSQL."
        actions={(
          <>
            <button className="btn-secondary" type="button" onClick={handleClearFilters} disabled={loading}>
              Limpiar
            </button>
            <button className="btn-primary" type="button" onClick={handleSearch} disabled={loading}>
              {loading ? 'Buscando...' : 'Buscar'}
            </button>
          </>
        )}
      >
        <div className="form-grid ventas-filter-grid">
          <div className="form-field">
            <label>Fecha desde</label>
            <input name="fechaDesde" type="date" value={filters.fechaDesde} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Fecha hasta</label>
            <input name="fechaHasta" type="date" value={filters.fechaHasta} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Tipo</label>
            <select name="tipoComprobanteVentaId" value={filters.tipoComprobanteVentaId} onChange={handleFilterChange}>
              <option value="">Todos</option>
              {tiposComprobante.map((tipo) => (
                <option key={tipo.id} value={tipo.id}>{tipo.descripcion}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label>Punto de venta</label>
            <input name="puntoVenta" type="number" min="1" value={filters.puntoVenta} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Numero</label>
            <input name="numeroComprobante" type="number" min="1" value={filters.numeroComprobante} onChange={handleFilterChange} />
          </div>
          <div className="form-field">
            <label>Estado</label>
            <select name="estado" value={filters.estado} onChange={handleFilterChange}>
              <option value="">Todos</option>
              <option value="Borrador">Borrador</option>
              <option value="Confirmada">Confirmada</option>
              <option value="Anulada">Anulada</option>
            </select>
          </div>
        </div>

        <ClienteObraSelector
          clienteExternoId={filters.clienteExternoId}
          obraExternoId={filters.obraExternaId}
          onChange={handleClienteObraFilterChange}
        />
      </SectionCard>

      <SectionCard title="Resultados" description={`${total} venta(s) encontrada(s).`}>
        {loading ? <LoadingSpinner /> : <VentasTable ventas={ventas} onEdit={setSelectedVenta} />}
        {error && <p className="form-error">{error}</p>}
      </SectionCard>
    </div>
  );
};

export default VentasHomePage;

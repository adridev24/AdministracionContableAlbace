import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import AcuerdosTable from '../components/AcuerdosTable';
import ClienteObraSelector from '../components/ClienteObraSelector';
import acuerdosService from '../services/acuerdosService';
import useAcuerdos from '../hooks/useAcuerdos';
import '../comercial.css';

const FILTERS_STORAGE_KEY = 'comercial.acuerdos.filters';

const getStoredFilters = () => {
  try {
    const stored = sessionStorage.getItem(FILTERS_STORAGE_KEY);
    if (!stored) return { clienteExternoId: '', obraExternoId: '' };

    const parsed = JSON.parse(stored);
    return {
      clienteExternoId: parsed.clienteExternoId ? String(parsed.clienteExternoId) : '',
      obraExternoId: parsed.obraExternoId ? String(parsed.obraExternoId) : '',
    };
  } catch {
    return { clienteExternoId: '', obraExternoId: '' };
  }
};

const AcuerdosPage = () => {
  const [filters, setFilters] = useState(getStoredFilters);
  const [exporting, setExporting] = useState(false);
  const { acuerdos, loading, error, fetchAcuerdosPorCliente } = useAcuerdos();

  const canSearchCliente = Boolean(filters.clienteExternoId);

  useEffect(() => {
    sessionStorage.setItem(FILTERS_STORAGE_KEY, JSON.stringify(filters));
  }, [filters]);

  useEffect(() => {
    if (filters.clienteExternoId) {
      fetchAcuerdosPorCliente(filters.clienteExternoId, filters.obraExternoId);
    }
  }, [fetchAcuerdosPorCliente, filters.clienteExternoId, filters.obraExternoId]);

  const handleFilterChange = (values) => {
    setFilters((prev) => ({
      ...prev,
      clienteExternoId: values.clienteExternoId ?? prev.clienteExternoId,
      obraExternoId: values.obraExternoId ?? values.obraExternaId ?? prev.obraExternoId,
    }));
  };

  const handleSearchCliente = () => {
    if (canSearchCliente) {
      fetchAcuerdosPorCliente(filters.clienteExternoId, filters.obraExternoId);
    }
  };

  const handleExportCsv = async () => {
    if (!acuerdos?.length) return;

    setExporting(true);
    try {
      const detalles = await Promise.all(
        acuerdos.map((acuerdo) => acuerdosService.getAcuerdoDetalle(acuerdo.id))
      );

      const rows = acuerdos.flatMap((acuerdo, index) => {
        const detalle = detalles[index];
        const vias = detalle?.vias?.length ? detalle.vias : acuerdo.vias ?? [];

        return vias.map((via) => {
          const ultimoPagoFecha = via.pagos?.length
            ? new Date(Math.max(...via.pagos.map((pago) => new Date(pago.fechaPago).getTime())))
            : null;

          return [
            acuerdo.clienteNombre || acuerdo.clienteExternoId || '',
            acuerdo.obraNombre || acuerdo.obraExternaId || '',
            via.viaOperacion || '',
            via.monedaCodigo || '',
            (via.montoActual ?? 0).toFixed(2),
            (via.totalPagado ?? 0).toFixed(2),
            (via.saldoPendiente ?? 0).toFixed(2),
            ultimoPagoFecha ? ultimoPagoFecha.toLocaleDateString() : '',
          ];
        });
      });

      const csvLines = [
        ['Cliente', 'Obra', 'Via', 'Moneda', 'Monto via', 'Total pagado', 'Saldo pendiente', 'Fecha ultimo pago'],
        ...rows,
      ].map((items) => items.map((value) => `"${String(value).replace(/"/g, '""')}"`).join(','));

      const csvBlob = new Blob([csvLines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
      const url = URL.createObjectURL(csvBlob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `acuerdos_export_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch (exportError) {
      console.error('Error exporting acuerdos:', exportError);
      alert('No se pudo generar el archivo. Intente nuevamente.');
    } finally {
      setExporting(false);
    }
  };

  const totalAcuerdos = acuerdos?.length ?? 0;
  const totalVias = acuerdos?.reduce((sum, acuerdo) => sum + (acuerdo.vias?.length ?? 0), 0) ?? 0;
  const estados = acuerdos?.reduce((acc, acuerdo) => {
    acc[acuerdo.estado] = (acc[acuerdo.estado] ?? 0) + 1;
    return acc;
  }, {}) ?? {};

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Acuerdos comerciales</h1>
          <p className="page-subtitle">Consulta acuerdos por cliente u obra y revisa el estado comercial antes del circuito contable.</p>
        </div>
        <div className="page-actions">
          <Link className="btn-secondary" to="/">Principal</Link>
          <Link className="btn-secondary" to="/comercial/reportes">Reportes</Link>
          <Link className="btn-primary" to="/comercial/nuevo">Nuevo acuerdo</Link>
        </div>
      </div>

      <SectionCard
        title="Buscar acuerdos"
        description="Selecciona un cliente y, si corresponde, una obra especifica."
        actions={(
          <button
            className="btn-primary"
            type="button"
            onClick={handleSearchCliente}
            disabled={!canSearchCliente || loading}
          >
            {loading ? 'Buscando...' : 'Ver acuerdos'}
          </button>
        )}
      >
        <ClienteObraSelector
          clienteExternoId={filters.clienteExternoId}
          obraExternoId={filters.obraExternoId}
          onChange={handleFilterChange}
        />
      </SectionCard>

      <div className="metric-strip">
        <div className="metric-item">
          <span>Acuerdos encontrados</span>
          <strong>{totalAcuerdos}</strong>
        </div>
        <div className="metric-item">
          <span>Vias comerciales</span>
          <strong>{totalVias}</strong>
        </div>
        <div className="metric-item">
          <span>Borradores</span>
          <strong>{estados.Borrador ?? 0}</strong>
        </div>
        <div className="metric-item">
          <span>En curso / aprobados</span>
          <strong>{(estados.EnCurso ?? 0) + (estados.Aprobado ?? 0)}</strong>
        </div>
      </div>

      <SectionCard
        title="Resultados"
        description="Listado operativo para abrir el detalle, revisar estado y exportar informacion."
        actions={(
          <button
            className="btn-secondary"
            type="button"
            onClick={handleExportCsv}
            disabled={!acuerdos?.length || loading || exporting}
          >
            {exporting ? 'Generando archivo...' : 'Exportar planilla'}
          </button>
        )}
      >
        {loading ? <LoadingSpinner /> : <AcuerdosTable acuerdos={acuerdos} />}
        {error && <p className="form-error">{error}</p>}
      </SectionCard>
    </div>
  );
};

export default AcuerdosPage;

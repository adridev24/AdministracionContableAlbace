import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import '../contabilidad.css';

const ContabilidadHomePage = () => (
  <div className="page-container contabilidad-page">
    <div className="page-header">
      <div>
        <h1>Contabilidad</h1>
        <p className="page-subtitle">Accesos operativos del modulo contable.</p>
      </div>
      <div className="page-actions">
        <Link className="btn-secondary" to="/">Principal</Link>
      </div>
    </div>

    <SectionCard title="Menu de Contabilidad">
      <div className="module-menu-grid">
        <Link className="module-menu-item" to="/contabilidad/cuentas">
          <strong>Plan de Cuentas</strong>
          <span>Administrar cuentas contables activas e historicas.</span>
        </Link>
        <Link className="module-menu-item" to="/contabilidad/asientos">
          <strong>Asientos Contables</strong>
          <span>Alta manual, consulta de detalle y reversion de asientos.</span>
        </Link>
        <Link className="module-menu-item" to="/contabilidad/configuracion">
          <strong>Configuracion Contable</strong>
          <span>Asociar operaciones del sistema con cuentas contables.</span>
        </Link>
      </div>
    </SectionCard>
  </div>
);

export default ContabilidadHomePage;

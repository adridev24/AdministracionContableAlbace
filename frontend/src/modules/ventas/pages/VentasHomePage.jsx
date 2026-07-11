import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import '../ventas.css';

const pendingFeatures = [
  'Comprobantes de venta',
  'Asociacion con anticipos y cuotas',
  'Pagos de Ventas',
  'Impacto contable automatico',
];

const VentasHomePage = () => (
  <div className="page-container ventas-page">
    <div className="page-header">
      <div>
        <h1>Ventas</h1>
        <p className="page-subtitle">Modulo independiente para operaciones formales de Via 1.</p>
      </div>
      <div className="page-actions">
        <Link className="btn-secondary" to="/">Principal</Link>
      </div>
    </div>

    <SectionCard
      title="Inicio de Ventas"
      description="Estructura inicial del modulo. Las funcionalidades se incorporaran progresivamente."
    >
      <div className="ventas-intro">
        <div>
          <span className="ventas-status">Modulo preparado</span>
          <h2>Base operativa inicial</h2>
          <p>
            Ventas queda separado de Comercial, Pagos y Contabilidad. En etapas posteriores podra
            registrar comprobantes de Via 1 y solicitar el impacto contable al servicio existente.
          </p>
        </div>
      </div>
    </SectionCard>

    <SectionCard title="Funcionalidades futuras">
      <div className="ventas-feature-grid">
        {pendingFeatures.map((feature) => (
          <button className="ventas-feature-item" disabled key={feature} type="button">
            <strong>{feature}</strong>
            <span>Pendiente</span>
          </button>
        ))}
      </div>
    </SectionCard>
  </div>
);

export default VentasHomePage;

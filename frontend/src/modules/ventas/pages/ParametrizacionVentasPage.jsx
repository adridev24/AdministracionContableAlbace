import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import '../ventas.css';

const ParametrizacionVentasPage = () => (
  <div className="page-container ventas-page">
    <div className="page-header">
      <div>
        <h1>Parametrizacion de Ventas</h1>
        <p className="page-subtitle">Configuracion de comprobantes, puntos de venta y parametros tributarios de Ventas.</p>
      </div>
      <div className="page-actions">
        <Link className="btn-secondary" to="/ventas">Ventas</Link>
        <Link className="btn-secondary" to="/">Principal</Link>
      </div>
    </div>

    <SectionCard title="Menu de Parametrizacion">
      <div className="module-menu-grid">
        <Link className="module-menu-item" to="/ventas/parametrizacion/comprobantes">
          <strong>Configuraciones de comprobantes</strong>
          <span>Administrar facturas, notas y modalidades fiscales disponibles.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/puntos-venta">
          <strong>Puntos de venta</strong>
          <span>Crear puntos y definir que comprobantes puede emitir cada uno.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/alicuotas-iva">
          <strong>Tratamientos y alicuotas de IVA</strong>
          <span>Administrar tratamientos gravados, exentos y no gravados.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/nomencladores-fce">
          <strong>Nomencladores FCE</strong>
          <span>Catalogo configurable para futuras facturas de credito electronica MiPyME.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/percepciones-iibb">
          <strong>Percepciones IIBB Entre Rios</strong>
          <span>Regimenes configurables para futuras percepciones en facturas de venta.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/categorias-items">
          <strong>Categorias de items facturables</strong>
          <span>Agrupadores opcionales para ordenar productos, servicios o conceptos.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/unidades-medida">
          <strong>Unidades de medida</strong>
          <span>Catalogo de unidades para items facturables con o sin decimales.</span>
        </Link>
        <Link className="module-menu-item" to="/ventas/parametrizacion/items-facturables">
          <strong>Items facturables</strong>
          <span>Catalogo independiente con IVA, unidad, nomenclador y precio por defecto.</span>
        </Link>
      </div>
    </SectionCard>
  </div>
);

export default ParametrizacionVentasPage;

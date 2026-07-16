import VentasHomePage from '../pages/VentasHomePage';
import VentaDetallePage from '../pages/VentaDetallePage';
import ParametrizacionVentasPage from '../pages/ParametrizacionVentasPage';
import ConfiguracionesComprobantePage from '../pages/ConfiguracionesComprobantePage';
import PuntosVentaPage from '../pages/PuntosVentaPage';
import AlicuotasIvaPage from '../pages/AlicuotasIvaPage';
import NomencladoresFcePage from '../pages/NomencladoresFcePage';
import PercepcionesIibbPage from '../pages/PercepcionesIibbPage';
import CategoriasItemsFacturablesPage from '../pages/CategoriasItemsFacturablesPage';
import UnidadesMedidaPage from '../pages/UnidadesMedidaPage';
import ItemsFacturablesPage from '../pages/ItemsFacturablesPage';

const ventasRoutes = [
  {
    path: '/ventas',
    element: <VentasHomePage />,
  },
  {
    path: '/ventas/:ventaId',
    element: <VentaDetallePage />,
  },
  {
    path: '/ventas/parametrizacion',
    element: <ParametrizacionVentasPage />,
  },
  {
    path: '/ventas/parametrizacion/comprobantes',
    element: <ConfiguracionesComprobantePage />,
  },
  {
    path: '/ventas/parametrizacion/puntos-venta',
    element: <PuntosVentaPage />,
  },
  {
    path: '/ventas/parametrizacion/alicuotas-iva',
    element: <AlicuotasIvaPage />,
  },
  {
    path: '/ventas/parametrizacion/nomencladores-fce',
    element: <NomencladoresFcePage />,
  },
  {
    path: '/ventas/parametrizacion/percepciones-iibb',
    element: <PercepcionesIibbPage />,
  },
  {
    path: '/ventas/parametrizacion/categorias-items',
    element: <CategoriasItemsFacturablesPage />,
  },
  {
    path: '/ventas/parametrizacion/unidades-medida',
    element: <UnidadesMedidaPage />,
  },
  {
    path: '/ventas/parametrizacion/items-facturables',
    element: <ItemsFacturablesPage />,
  },
];

export default ventasRoutes;

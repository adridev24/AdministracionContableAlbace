import VentasHomePage from '../pages/VentasHomePage';
import ParametrizacionVentasPage from '../pages/ParametrizacionVentasPage';
import ConfiguracionesComprobantePage from '../pages/ConfiguracionesComprobantePage';
import PuntosVentaPage from '../pages/PuntosVentaPage';
import AlicuotasIvaPage from '../pages/AlicuotasIvaPage';
import NomencladoresFcePage from '../pages/NomencladoresFcePage';
import PercepcionesIibbPage from '../pages/PercepcionesIibbPage';

const ventasRoutes = [
  {
    path: '/ventas',
    element: <VentasHomePage />,
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
];

export default ventasRoutes;

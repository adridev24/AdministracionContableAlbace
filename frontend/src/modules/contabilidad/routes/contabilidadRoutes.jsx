import PlanCuentasPage from '../pages/PlanCuentasPage';
import ContabilidadHomePage from '../pages/ContabilidadHomePage';
import AsientosContablesPage from '../pages/AsientosContablesPage';
import NuevoAsientoPage from '../pages/NuevoAsientoPage';
import AsientoDetallePage from '../pages/AsientoDetallePage';
import ConfiguracionContablePage from '../pages/ConfiguracionContablePage';

const contabilidadRoutes = [
  {
    path: '/contabilidad',
    element: <ContabilidadHomePage />,
  },
  {
    path: '/contabilidad/cuentas',
    element: <PlanCuentasPage />,
  },
  {
    path: '/contabilidad/asientos',
    element: <AsientosContablesPage />,
  },
  {
    path: '/contabilidad/asientos/nuevo',
    element: <NuevoAsientoPage />,
  },
  {
    path: '/contabilidad/asientos/:id',
    element: <AsientoDetallePage />,
  },
  {
    path: '/contabilidad/configuracion',
    element: <ConfiguracionContablePage />,
  },
];

export default contabilidadRoutes;

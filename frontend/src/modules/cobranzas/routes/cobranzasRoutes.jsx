import CobranzasPage from '../pages/CobranzasPage';
import CarteraChequesPage from '../pages/CarteraChequesPage';

const cobranzasRoutes = [
  {
    path: '/ventas/cobranzas',
    element: <CobranzasPage />,
  },
  {
    path: '/ventas/cartera-cheques',
    element: <CarteraChequesPage />,
  },
];

export default cobranzasRoutes;

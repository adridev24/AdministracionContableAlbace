import React from 'react';
import AcuerdosPage from '../pages/AcuerdosPage';
import AcuerdoDetallePage from '../pages/AcuerdoDetallePage';
import NuevoAcuerdoPage from '../pages/NuevoAcuerdoPage';
import PagosComercialesPage from '../pages/PagosComercialesPage';
import ReportesComercialesPage from '../pages/ReportesComercialesPage';

const comercialRoutes = [
  {
    path: '/comercial',
    element: <AcuerdosPage />
  },
  {
    path: '/comercial/nuevo',
    element: <NuevoAcuerdoPage />
  },
  {
    path: '/comercial/:id',
    element: <AcuerdoDetallePage />
  },
  {
    path: '/comercial/pagos',
    element: <PagosComercialesPage />
  },
  {
    path: '/comercial/reportes',
    element: <ReportesComercialesPage />
  }
];

export default comercialRoutes;

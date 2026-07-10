import { useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './Components/auth/Login';
import Dashboard from './Components/Dashboard';
import { comercialRoutes } from './modules/comercial';
import { contabilidadRoutes } from './modules/contabilidad';

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  // Esta función se la pasaremos al Login para que nos avise cuando entrar
  const handleLoginSuccess = () => {
    setIsAuthenticated(true);
  };

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={isAuthenticated ? <Navigate to="/" /> : <Login onLoginSuccess={handleLoginSuccess} />} />
        <Route path="/" element={isAuthenticated ? <Dashboard /> : <Navigate to="/login" />} />
        {comercialRoutes.map((route) => (
          <Route
            key={route.path}
            path={route.path}
            element={isAuthenticated ? route.element : <Navigate to="/login" />}
          />
        ))}
        {contabilidadRoutes.map((route) => (
          <Route
            key={route.path}
            path={route.path}
            element={isAuthenticated ? route.element : <Navigate to="/login" />}
          />
        ))}
        <Route path="*" element={<Navigate to={isAuthenticated ? '/' : '/login'} />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

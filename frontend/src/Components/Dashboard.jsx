import { useNavigate } from 'react-router-dom';
import logoAlbace from '../assets/logo_albace_completo.png';
import { getCurrentUser } from '../Services/authUtils';

const modules = [
  {
    title: 'Comercial',
    description: 'Acuerdos, planes de pago y condiciones comerciales.',
    status: 'Disponible',
    route: '/comercial',
    metric: '',
  },
  {
    title: 'Compras',
    description: 'Gestión de insumos, proveedores y comprobantes.',
    status: 'Próximo',
    route: null,
    metric: 'Pendiente de definición',
  },
  {
    title: 'Ventas',
    description: 'Modulo inicial para operaciones formales de Via 1.',
    status: 'Disponible',
    route: '/ventas',
    metric: 'Estructura inicial',
  },
  {
    title: 'Contabilidad',
    description: 'Plan de cuentas y asientos contables.',
    status: 'Disponible',
    route: '/contabilidad',
    metric: 'Plan de Cuentas / Asientos',
  },
];

const Dashboard = () => {
  const navigate = useNavigate();
  const currentUser = getCurrentUser();

  const handleLogout = () => {
    localStorage.removeItem('token');
    navigate('/login');
  };

  return (
    <div className="dashboard-page">
      <header className="dashboard-topbar">
        <img src={logoAlbace} alt="Metalúrgica Albace" className="dashboard-logo" />
        <div className="dashboard-actions">
          {currentUser?.username && <span className="session-user">{currentUser.fullName || currentUser.username}</span>}
          <button onClick={handleLogout} className="dashboard-button-secondary">Cerrar sesion</button>
        </div>
      </header>

      <section className="dashboard-hero">
        <div className="dashboard-hero-copy">
          <span className="eyebrow">Sistema administrativo</span>
          <h1>Panel de control operativo</h1>
        </div>
      </section>

      <section className="dashboard-section-heading">
        <div>
          <span className="eyebrow">Módulos</span>
          <h2>Accesos principales</h2>
        </div>
      </section>

      <div className="dashboard-cards">
        {modules.map((module) => (
          <button
            key={module.title}
            className={`dashboard-card ${module.route ? 'is-active' : 'is-disabled'}`}
            onClick={() => module.route && navigate(module.route)}
            type="button"
            disabled={!module.route}
          >
            <div className="dashboard-card-header">
              <span className="module-mark">{module.title.slice(0, 2).toUpperCase()}</span>
              <span className="module-status">{module.status}</span>
            </div>
            <h3>{module.title}</h3>
            <p>{module.description}</p>
            <small>{module.metric}</small>
          </button>
        ))}
      </div>
    </div>
  );
};

export default Dashboard;

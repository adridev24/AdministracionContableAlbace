import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import AcuerdoForm from '../components/AcuerdoForm';
import acuerdosService from '../services/acuerdosService';
import '../comercial.css';

const NuevoAcuerdoPage = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (payload) => {
    setLoading(true);
    setError('');
    setSuccess('');

    try {
      const acuerdo = await acuerdosService.createAcuerdo(payload);
      setSuccess('Acuerdo comercial creado correctamente.');
      setTimeout(() => navigate(`/comercial/${acuerdo.id}`), 800);
    } catch (err) {
      setError('No fue posible crear el acuerdo. Revisa los datos e intenta de nuevo.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Nuevo acuerdo comercial</h1>
          <p className="page-subtitle">Crea el compromiso comercial de una obra.</p>
        </div>
        <div className="header-actions">
          <button className="btn-secondary" type="button" onClick={() => navigate(-1)}>
            Cancelar
          </button>
        </div>
      </div>

      <section className="card-panel">
        <h2>Datos del acuerdo</h2>
        <p>Completa los datos para registrar un nuevo acuerdo comercial.</p>
        <AcuerdoForm onSubmit={handleSubmit} loading={loading} />
        {error && <p className="form-error">{error}</p>}
        {success && <p className="form-success">{success}</p>}
      </section>
    </div>
  );
};

export default NuevoAcuerdoPage;

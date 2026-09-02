import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import SectionCard from '../../../shared/components/SectionCard';
import LoadingSpinner from '../../../shared/components/LoadingSpinner';
import PagoComercialForm from '../components/PagoComercialForm';
import acuerdosService from '../services/acuerdosService';
import usePagosComerciales from '../hooks/usePagosComerciales';
import '../comercial.css';

const PagosComercialesPage = () => {
  const [acuerdoId, setAcuerdoId] = useState('');
  const [acuerdo, setAcuerdo] = useState(null);
  const [loadingDetalle, setLoadingDetalle] = useState(false);
  const [detalleError, setDetalleError] = useState('');
  const { loading, error, success, registerPago, setError, setSuccess } = usePagosComerciales();

  const handleSearch = async (e) => {
    e.preventDefault();
    setDetalleError('');
    if (!acuerdoId.trim()) {
      setDetalleError('Ingresa un ID de acuerdo válido.');
      return;
    }
    setLoadingDetalle(true);

    try {
      const data = await acuerdosService.getAcuerdoDetalle(acuerdoId.trim());
      setAcuerdo(data);
      setSuccess('');
    } catch (err) {
      setDetalleError('No se encontró el acuerdo. Comprueba el ID e intenta de nuevo.');
    } finally {
      setLoadingDetalle(false);
    }
  };

  const handleSubmitPago = async (payload) => {
    setError('');
    try {
      await registerPago(payload);
      const data = await acuerdosService.getAcuerdoDetalle(payload.acuerdoComercialId);
      setAcuerdo(data);
    } catch (_) {}
  };

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1>Registro de pagos comerciales</h1>
          <p className="page-subtitle">Carga pagos y aplica importes contra las cuotas del acuerdo.</p>
        </div>
        <Link className="btn-secondary" to="/comercial">Volver a acuerdos</Link>
      </div>

      <SectionCard title="Buscar acuerdo" description="Crea un pago comercial para el acuerdo seleccionado.">
        <form className="search-row" onSubmit={handleSearch}>
          <input
            value={acuerdoId}
            onChange={(e) => setAcuerdoId(e.target.value)}
            placeholder="ID del acuerdo comercial"
          />
          <button className="btn-secondary" type="submit">Cargar acuerdo</button>
        </form>
        {detalleError && <p className="form-error">{detalleError}</p>}
      </SectionCard>

      {loadingDetalle ? (
        <LoadingSpinner />
      ) : acuerdo ? (
        <SectionCard title="Formulario de pago" description="Registra el pago comercial y aplica el importe a las cuotas.">
          <PagoComercialForm
            acuerdo={acuerdo}
            onSubmit={handleSubmitPago}
            loading={loading}
            error={error || success ? '' : ''}
          />
          {error && <p className="form-error">{error}</p>}
          {success && <p className="form-success">{success}</p>}
        </SectionCard>
      ) : (
        <SectionCard title="Acuerdo no seleccionado" description="Busca un acuerdo para cargar pagos comerciales.">
          <p className="empty-state">Ingresa un ID de acuerdo para consultar el detalle y registrar el pago.</p>
        </SectionCard>
      )}
    </div>
  );
};

export default PagosComercialesPage;

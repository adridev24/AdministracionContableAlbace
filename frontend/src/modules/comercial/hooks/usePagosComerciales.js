import { useState } from 'react';
import pagosComercialesService from '../services/pagosComercialesService';

const usePagosComerciales = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const registerPago = async (payload) => {
    setLoading(true);
    setError('');
    setSuccess('');
    try {
      const data = await pagosComercialesService.registrarPago(payload);
      setSuccess('Pago comercial registrado correctamente.');
      return data;
    } catch (err) {
      setError('No fue posible registrar el pago. Revisa los importes y vuelve a intentar.');
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const applyPago = async (pagoId, payload) => {
    setLoading(true);
    setError('');
    setSuccess('');
    try {
      const data = await pagosComercialesService.aplicarPago(pagoId, payload);
      setSuccess('Pago aplicado correctamente a las cuotas.');
      return data;
    } catch (err) {
      setError('No fue posible aplicar el pago. Verifica los saldos y vuelve a intentarlo.');
      throw err;
    } finally {
      setLoading(false);
    }
  };

  return {
    loading,
    error,
    success,
    registerPago,
    applyPago,
    setError,
    setSuccess
  };
};

export default usePagosComerciales;

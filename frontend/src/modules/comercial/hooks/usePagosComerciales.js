import { useState } from 'react';
import pagosComercialesService from '../services/pagosComercialesService';

const getApiError = (err, fallback) => {
  const message = err?.response?.data?.error;
  return typeof message === 'string' && message.trim() ? message : fallback;
};

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
      setError(getApiError(err, 'No fue posible registrar el pago. Revisa los importes y vuelve a intentar.'));
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
      setError(getApiError(err, 'No fue posible aplicar el pago. Verifica los saldos y vuelve a intentarlo.'));
      throw err;
    } finally {
      setLoading(false);
    }
  };

  const anularPago = async (pagoId, payload) => {
    setLoading(true);
    setError('');
    setSuccess('');
    try {
      const data = await pagosComercialesService.anularPago(pagoId, payload);
      setSuccess('Pago anulado correctamente.');
      return data;
    } catch (err) {
      setError(getApiError(err, 'No fue posible anular el pago.'));
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
    anularPago,
    setError,
    setSuccess
  };
};

export default usePagosComerciales;

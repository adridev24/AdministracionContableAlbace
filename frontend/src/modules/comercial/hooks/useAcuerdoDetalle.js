import { useEffect, useState } from 'react';
import acuerdosService from '../services/acuerdosService';

const useAcuerdoDetalle = (acuerdoId) => {
  const [detalle, setDetalle] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadDetalle = async () => {
      if (!acuerdoId) return;
      setLoading(true);
      setError('');
      try {
        const data = await acuerdosService.getAcuerdoDetalle(acuerdoId);
        setDetalle(data);
      } catch (err) {
        setError('No fue posible cargar el detalle del acuerdo.');
      } finally {
        setLoading(false);
      }
    };

    loadDetalle();
  }, [acuerdoId]);

  return { detalle, loading, error, setDetalle };
};

export default useAcuerdoDetalle;

import { useState, useCallback } from 'react';
import acuerdosService from '../services/acuerdosService';
import externalDataService from '../services/externalDataService';

const enrichAcuerdosWithNames = async (acuerdos) => {
  const clientNameCache = {};
  const obraNameCache = {};

  return await Promise.all(acuerdos.map(async (acuerdo) => {
    const enriched = { ...acuerdo };
    const clienteId = Number(acuerdo.clienteExternoId);
    const obraId = Number(acuerdo.obraExternaId);

    if (!Number.isNaN(clienteId)) {
      if (!(clienteId in clientNameCache)) {
        try {
          const client = await externalDataService.getClientById(clienteId);
          clientNameCache[clienteId] = client?.nombreCliente || null;
        } catch {
          clientNameCache[clienteId] = null;
        }
      }
      if (clientNameCache[clienteId]) {
        enriched.clienteNombre = clientNameCache[clienteId];
      }
    }

    if (!Number.isNaN(obraId)) {
      if (!(obraId in obraNameCache)) {
        try {
          const obra = await externalDataService.getObraById(obraId);
          obraNameCache[obraId] = obra?.nombreObra || null;
        } catch {
          obraNameCache[obraId] = null;
        }
      }
      if (obraNameCache[obraId]) {
        enriched.obraNombre = obraNameCache[obraId];
      }
    }

    return enriched;
  }));
};

const useAcuerdos = () => {
  const [acuerdos, setAcuerdos] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const fetchAcuerdosPorCliente = useCallback(async (clienteExternoId, obraExternoId = '') => {
    setLoading(true);
    setError('');

    try {
      const data = await acuerdosService.getAcuerdosPorCliente(clienteExternoId);
      const filtered = obraExternoId
        ? data.filter((acuerdo) => String(acuerdo.obraExternaId) === String(obraExternoId))
        : data;
      const enriched = await enrichAcuerdosWithNames(filtered);
      setAcuerdos(enriched);
    } catch (err) {
      setError('No fue posible cargar los acuerdos del cliente.');
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchAcuerdosPorObra = useCallback(async (obraExternaId) => {
    setLoading(true);
    setError('');

    try {
      const data = await acuerdosService.getAcuerdosPorObra(obraExternaId);
      const enriched = await enrichAcuerdosWithNames(data);
      setAcuerdos(enriched);
    } catch (err) {
      setError('No fue posible cargar los acuerdos de la obra.');
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    acuerdos,
    loading,
    error,
    fetchAcuerdosPorCliente,
    fetchAcuerdosPorObra,
    setAcuerdos
  };
};

export default useAcuerdos;

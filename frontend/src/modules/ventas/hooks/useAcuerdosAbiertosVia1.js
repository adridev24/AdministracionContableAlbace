import { useCallback, useEffect, useState } from 'react';
import ventasService from '../services/ventasService';
import { estadoAcuerdosAbiertos } from './estadoAcuerdosAbiertos.js';

export default function useAcuerdosAbiertosVia1(cliente, obra, moneda, ventaActual = 0) {
  const [result, setResult] = useState({ key: '', rows: [], error: '' });
  const [revision, setRevision] = useState(0);
  const key = JSON.stringify([cliente, obra, moneda, ventaActual, revision]);
  useEffect(() => {
    if (!cliente || !obra) return;
    let active = true;
    ventasService.acuerdosAbiertosVia1({ cliente, obra, moneda, ventaActual })
      .then(rows => { if (active) setResult({ key, rows, error: '' }); })
      .catch(() => { if (active) setResult({ key, rows: [], error: 'No se pudieron consultar los acuerdos abiertos de Vía 1. Actualizá la consulta antes de guardar.' }); });
    return () => { active = false; };
  }, [cliente, obra, moneda, ventaActual, key]);
  const refresh = useCallback(() => setRevision(r => r + 1), []);
  useEffect(() => {
    if (!cliente || !obra) return;
    window.addEventListener('focus', refresh);
    return () => window.removeEventListener('focus', refresh);
  }, [cliente, obra, refresh]);
  return { ...estadoAcuerdosAbiertos(Boolean(cliente && obra), key, result), refresh };
}

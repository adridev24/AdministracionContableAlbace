import React, { useEffect, useMemo, useState } from 'react';
import externalDataService from '../services/externalDataService';

const ObraSelector = ({ obraExternoId, onChange }) => {
  const [obras, setObras] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    externalDataService.getObras()
      .then((data) => {
        if (mounted) setObras(data || []);
      })
      .finally(() => {
        if (mounted) setLoading(false);
      });
    return () => { mounted = false; };
  }, []);

  const filteredObras = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return obras;
    return obras.filter((obra) =>
      obra.nombreObra?.toLowerCase().includes(term) ||
      obra.descripcion?.toLowerCase().includes(term)
    );
  }, [obras, search]);

  const selectedObra = obras.find((obra) => String(obra.idObra) === String(obraExternoId));

  const handleObraChange = (event) => {
    onChange({ obraExternoId: event.target.value, clienteExternoId: '' });
  };

  return (
    <div className="client-obra-selector">
      <div className="selector-grid">
        <div className="form-field">
          <label>Buscar obra</label>
          <input
            type="text"
            placeholder="Buscar por nombre o descripción"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="form-field">
          <label>Obra</label>
          <select value={obraExternoId} onChange={handleObraChange} disabled={loading || filteredObras.length === 0}>
            <option value="">Selecciona una obra</option>
            {filteredObras.map((obra) => (
              <option key={obra.idObra} value={String(obra.idObra)}>
                {obra.nombreObra} {obra.descripcion ? `- ${obra.descripcion}` : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {loading && <p className="hint-text">Cargando obras...</p>}
      {!loading && filteredObras.length === 0 && (
        <p className="hint-text">No se encontraron obras para esa búsqueda.</p>
      )}

      {selectedObra && (
        <div className="client-card">
          <strong>{selectedObra.nombreObra}</strong>
          <p>{selectedObra.descripcion}</p>
          <p>Finalizada: {selectedObra.finalizada}</p>
        </div>
      )}
    </div>
  );
};

export default ObraSelector;


import React, { useEffect, useMemo, useState } from 'react';
import externalDataService from '../services/externalDataService';

const ClienteObraSelector = ({ clienteExternoId, obraExternoId, onChange }) => {
  const [clients, setClients] = useState([]);
  const [obras, setObras] = useState([]);
  const [search, setSearch] = useState('');
  const [loadingClients, setLoadingClients] = useState(true);
  const [loadingObras, setLoadingObras] = useState(false);

  useEffect(() => {
    let mounted = true;
    externalDataService.getClients()
      .then((data) => {
        if (mounted) setClients(data || []);
      })
      .finally(() => {
        if (mounted) setLoadingClients(false);
      });
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    if (!clienteExternoId) {
      setObras([]);
      return;
    }

    setLoadingObras(true);
    let mounted = true;
    externalDataService.getObrasByClient(clienteExternoId)
      .then((data) => {
        if (mounted) setObras(data || []);
      })
      .finally(() => {
        if (mounted) setLoadingObras(false);
      });

    return () => { mounted = false; };
  }, [clienteExternoId]);

  const filteredClients = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return clients;
    return clients.filter((client) =>
      client.nombreCliente?.toLowerCase().includes(term) ||
      client.domicilio?.toLowerCase().includes(term) ||
      client.telefonoc?.toLowerCase().includes(term)
    );
  }, [clients, search]);

  const normalizedClienteId = clienteExternoId != null ? String(clienteExternoId) : '';
  const normalizedObraId = obraExternoId != null ? String(obraExternoId) : '';
  const selectedClient = clients.find((client) => String(client.idCliente) === normalizedClienteId);
  const selectedObra = obras.find((obra) => String(obra.idObra) === normalizedObraId);

  const handleClientChange = (event) => {
    const clienteId = String(event.target.value);
    onChange({ clienteExternoId: clienteId, obraExternoId: '', obraExternaId: '' });
  };

  const handleObraChange = (event) => {
    const obraId = String(event.target.value);
    onChange({ clienteExternoId: normalizedClienteId, obraExternoId: obraId, obraExternaId: obraId });
  };

  return (
    <div className="client-obra-selector">
      <div className="selector-grid">
        <div className="form-field">
          <label>Cliente</label>
          <select value={clienteExternoId} onChange={handleClientChange} disabled={loadingClients || filteredClients.length === 0}>
            <option value="">Selecciona un cliente</option>
            {filteredClients.map((client) => (
              <option key={client.idCliente} value={String(client.idCliente)}>
                {client.nombreCliente} {client.telefonoc ? `· ${client.telefonoc}` : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {loadingClients && <p className="hint-text">Cargando clientes...</p>}
      {!loadingClients && filteredClients.length === 0 && (
        <p className="hint-text">No se encontraron clientes para esa búsqueda.</p>
      )}

      {selectedClient && (
        <div className="client-card">
          <strong>{selectedClient.nombreCliente}</strong>
          <p>{selectedClient.domicilio}</p>
          <p>{selectedClient.telefonoc}</p>
        </div>
      )}

      <div className="form-field">
        <label>Obra</label>
        <select value={String(obraExternoId || '')} onChange={handleObraChange} disabled={!selectedClient || loadingObras || obras.length === 0}>
          <option value="">Selecciona una obra</option>
          {obras.map((obra) => (
            <option key={obra.idObra} value={String(obra.idObra)}>
              {obra.nombreObra} {obra.descripcion ? `- ${obra.descripcion}` : ''}
            </option>
          ))}
        </select>
      </div>

      {loadingObras && <p className="hint-text">Cargando obras para este cliente...</p>}
      {!loadingObras && selectedClient && obras.length === 0 && (
        <p className="hint-text">Este cliente no tiene obras registradas en el servidor SQL.</p>
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

export default ClienteObraSelector;

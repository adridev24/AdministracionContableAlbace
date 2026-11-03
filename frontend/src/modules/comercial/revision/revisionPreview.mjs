// Isolated visual test harness: synthetic data, no API calls, no authentication or real database.
import { createServer } from 'vite';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../../../../', import.meta.url));
const server = await createServer({ root, server: { host: '127.0.0.1', port: 5187, strictPort: true }, plugins: [{
  name: 'revision-visual-fixture',
  configureServer(vite) {
    vite.middlewares.use('/__revision_preview', async (_req, res) => {
      const html = await vite.transformIndexHtml('/__revision_preview', `<!doctype html><html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Prueba visual de revisión</title><div id="root"></div>
        <script type="module">
          import React from 'react';
          import { createRoot } from 'react-dom/client';
          import Panel from '/src/modules/comercial/components/RevisionPlanPanel.jsx';
          import { preparationFixture } from '/src/modules/comercial/revision/revisionFixtures.js';
          import '/src/modules/comercial/comercial.css';
          const data = preparationFixture();
          data.cuotas[2].puedeModificar = false; data.cuotas[2].motivoBloqueoModificacion = 'Tiene una factura activa; requiere anulación formal previa.';
          const api = { prepare: async () => data, confirm: async () => ({ revision: { id: 100 }, estadoActual: data }) };
          createRoot(document.getElementById('root')).render(React.createElement(Panel, { acuerdoId: 10, viaId: 20, planId: 30, api, onClose: () => location.reload(), onSuccess: async () => {} }));
        </script></html>`);
      res.setHeader('Content-Type', 'text/html'); res.end(html);
    });
  }
}] });
await server.listen();
console.log('Synthetic preview: http://127.0.0.1:5187/__revision_preview');

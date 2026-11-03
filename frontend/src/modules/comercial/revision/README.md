# Revisión de planes aprobados

La acción está en el plan de la vía seleccionada en `AcuerdoDetallePage`. Usa el
rol exacto `Admin` del JWT mediante `getUserRoles`; la API sigue autorizando cada
operación. El editor general permanece de solo lectura después de aprobar.

`RevisionPlanPanel` presenta la preparación y el resumen. `revisionDraft` mantiene
el borrador, calcula con centavos enteros (BigInt) y construye exclusivamente el
DTO del backend. `revisionSession` controla carga, confirmación, errores y
reintentos. `revisionPlanService` utiliza el cliente HTTP autenticado existente.

Las versiones se conservan en el borrador y se envían sin reinterpretarlas. Para
habilitar la siguiente cuota tras un retiro pendiente, solo se admite el motivo
de secuencia exacto del contrato actual y ausencia de bloqueo de modificación.
Un motivo desconocido mantiene el retiro bloqueado. El backend revalida todo.

Una respuesta ambigua congela el payload y su UUID en la sesión del modal; no
permite editar, cerrar ni recargar sus datos hasta resolver el reintento. No se
persiste el borrador entre recargas completas del navegador. Un 409 conserva el
borrador y exige una recarga manual con advertencia; nunca actualiza versiones
ni reenvía automáticamente. Tras éxito se consulta nuevamente el plan y el
detalle del acuerdo. No se modifica MontoOriginal ni se crean créditos locales.

Desde la raíz del repositorio, ejecutar todas las pruebas frontend:

```powershell
node --test frontend/src/modules/comercial/revision/revisionDraft.test.js frontend/src/modules/comercial/revision/revisionSession.test.js frontend/src/modules/comercial/revision/revisionView.test.js frontend/src/shared/utils/calendarDate.test.js frontend/src/modules/comercial/services/reportFilters.test.js
```

Las pruebas de vista renderizan React con Vite SSR usando las dependencias
existentes. Las pruebas de sesión utilizan respuestas HTTP simuladas; las suites
backend verifican por separado la API y PostgreSQL.

Para inspección manual aislada, sin autenticación, API ni bases reales:

```powershell
node frontend/src/modules/comercial/revision/revisionPreview.mjs
```

Abrir `http://127.0.0.1:5187/__revision_preview`. Utiliza datos sintéticos y una
confirmación simulada; no sustituye una prueba integral con el backend. El
ejecutor solo escucha en loopback y no forma parte del bundle de producción.

Los reportes quedan para el bloque correspondiente: verificar su presentación
de MontoActual, obligaciones vigentes y saldo a favor después de revisiones.

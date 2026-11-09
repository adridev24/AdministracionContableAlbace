# Reportes contables e impositivos — ventas

Acceso: panel principal → **Reportes fiscales** (`/reportes-fiscales`). Requiere sesión autenticada, como los módulos contables existentes.

## Alcance

- Datos propios de una empresa: CUIT, razón social, domicilio, condición de IVA, inscripción IIBB, actividad e inicio de actividades.
- Libro mensual de ventas confirmadas, tanto de carga simple como detallada, con signo de notas de crédito, desglose por alícuota y resúmenes en pesos.
- CSV UTF-8 para Excel e impresión del libro/retenciones desde el navegador (incluye guardar como PDF).
- ZIP de ventas para IVA Simple: dos TXT ANSI Windows-1252 con CRLF, respaldo JSON e instrucciones. Importes en **moneda original del comprobante**; seleccionar esa opción en Portal IVA.
- Retenciones sufridas provenientes de medios de cobro `RETENCION_*` de cobranzas confirmadas. Los certificados complementan registros existentes; no crean movimientos ni permiten modificar importes.
- Conciliación de ventas con archivos de comprobantes y alícuotas del diseño ARCA. Conciliación de retenciones con una plantilla CSV normalizada propia, no con cualquier descarga arbitraria de Mis Retenciones.
- Historial inmutable de archivos generados: descarga idéntica, usuario, fecha y SHA-256. El ZIP guarda también los datos fiscales usados al generarlo.

No incluye compras ni carga manual sustitutiva, presentación de DDJJ, emisión de comprobantes, cálculo de saldo completo de IVA, archivos SICORE/SIRE de agentes ni liquidación de IIBB. El módulo no escribe en ventas, cobranzas o contabilidad.

## Preparación y uso

1. Aplicar la migración **ReportesFiscalesVentas** sobre una base que ya tenga **FacturasHitosVia1**. El cambio crea cuatro tablas `reportes_*`, sus índices y relaciones; no actualiza datos operativos. El inicializador existente usa `EnsureCreated`, por lo que reiniciar la API no migra una base existente.
   El script de actualización está en `backend/sql/reportes_fiscales_ventas.sql`; incluye transacción y registro en el historial de EF. Ejecutarlo una sola vez sobre la base de destino, después de verificar su versión. No ejecutar todas las migraciones históricas sobre una base inicializada por otros mecanismos.
2. Configurar la empresa. IVA Simple se habilita para responsable inscripto. Una vez generadas exportaciones no se permite cambiar su CUIT.
3. Consultar el mes y completar los datos fiscales de cada venta con los del comprobante original. La referencia local del cliente ofrece una sugerencia, nunca se considera confirmación automática. La ficha es por comprobante para preservar su contexto fiscal.
4. Resolver los errores de empresa/ventas y revisar las advertencias. Revisar específicamente devoluciones de cheques incluidas en conceptos no gravados.
5. Completar los certificados de las retenciones sufridas. La fecha de cobranza, la fecha del certificado y el período de cómputo son datos distintos.
6. Conciliar contra los archivos de referencia. Ingresar el CUIT al que pertenecen: el TXT de ventas no lleva el CUIT del emisor y no permite comprobarlo automáticamente.
7. Generar el ZIP, extraerlo y cargar ambos TXT en Portal IVA como importes en moneda original. Revisar la respuesta del organismo. Generar o descargar no registra una presentación ni una aceptación de ARCA.

## Validaciones y límites explícitos

- Solo ventas confirmadas. Borradores y anulaciones locales se muestran como advertencias de exclusión; no se infiere una anulación fiscal ni se genera un archivo de anulaciones a partir del estado interno.
- No se genera exportación vacía ni se declara automáticamente «sin movimiento».
- CUIT/CUIL con dígito verificador; DNI de 7/8 dígitos. Otros tipos de documento, compradores sin identificar y regímenes especiales requieren una ampliación validada.
- Tipos ordinarios A/B/E y FCE A del catálogo existente; equivalencias de códigos conocidas. Notas genéricas requieren identificar la letra. Los tipos C se muestran pero bloquean la exportación para responsable inscripto. Nuevos tipos operativos sin equivalencia verificada quedan bloqueados.
- Monedas ARS/PES, USD/DOL y EUR/060. Cotización positiva de hasta cuatro enteros y seis decimales; pesos exige uno. No se usa la referencia comercial del dólar de una factura emitida en pesos.
- Alícuotas 0, 2,5, 5, 10,5, 21 y 27%; agrupadas por porcentaje. Operación íntegramente exenta/no gravada: registro de alícuota cero con neto cero. El código de operación debe ser explícito si no hay IVA.
- Notas de crédito negativas en el libro y positivas en los TXT, identificadas por su tipo.
- Totales vs componentes y cabecera vs alícuotas: tolerancia un centavo. IVA por porcentaje: dos centavos. Se bloquean importes fuera del ancho, caracteres no representables en Windows-1252, documento inválido y percepciones inconsistentes.
- Los nombres se conservan completos en el respaldo y se limitan a los 30 caracteres del campo oficial en el TXT.
- Límite de 10 MB por TXT. Volúmenes superiores requieren futura división en lotes; no se truncan.
- La conciliación de ventas exige archivos individuales de 266/62 caracteres y moneda original. Rechaza fechas fuera del mes, líneas mal formadas y rangos diarios; informa faltantes, duplicados y diferencias.
- La plantilla de retenciones usa `impuesto;cuit_agente;certificado;regimen;jurisdiccion;fecha;importe_ars`, UTF-8, fecha `AAAA-MM-DD`, importe positivo sin miles (punto o coma decimal). Los certificados se comparan por impuesto, CUIT agente, certificado, régimen y jurisdicción, y se controlan fecha e importe. Se comparan los certificados asignados al período de cómputo; los incompletos aparecen como pendientes.
- Los datos complementarios tienen control de concurrencia. Modificar una ficha no cambia los ZIP ya generados; consultar nuevamente muestra los datos actuales.

## Fuentes del formato

Consultadas el 08/11/2026:

- [IVA Simple — registración electrónica](https://www.afip.gob.ar/iva/iva-simple/especificaciones-especiales.asp).
- [Diseños de registro — ventas 266 y alícuotas 62](https://www.afip.gob.ar/libro-iva-digital/documentos/libro-iva-digital-diseno-registros.pdf).
- [Especificaciones de campos](https://www.afip.gob.ar/iva/documentos/libro-iva-digital-especificaciones.pdf).
- [Tablas oficiales](https://www.afip.gob.ar/iva/documentos/Libro-IVA-Digital-Tablas-del-Sistema.pdf).

La validación local y las pruebas de formato no equivalen a una importación aceptada en el Portal IVA del contribuyente.

## Verificación

```powershell
dotnet run --project tests/ReportesFiscales.Tests
dotnet run --project tests/ReportesFiscales.Tests -- backend/appsettings.json
dotnet run --project tests/PlanRevision.Tests -- backend/appsettings.json --hitos
```

La segunda ejecución requiere PostgreSQL local y permiso para crear una base descartable. Rechaza hosts no locales y nunca migra la base configurada. Prueba la migración real sobre el esquema anterior, datos fiscales, bloqueo de exportación, ZIP, historial, períodos de retenciones y conservación de operaciones. Elimina únicamente su base aleatoria en `finally`.

En `frontend`: `npm run build` y `npx eslint src/modules/reportesFiscales src/App.jsx src/Components/Dashboard.jsx`.

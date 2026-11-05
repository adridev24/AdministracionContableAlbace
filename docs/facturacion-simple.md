# Facturación simple y Libro IVA Ventas

La factura se registra por cliente y obra, sin productos ni ítems ficticios.
La primera línea fiscal pide neto y alícuota activa de IVA; una segunda línea
opcional permite otra tasa. El servidor calcula cada IVA a centavos y conserva
en la factura la base, alícuota e importe usados, independientes de cambios
posteriores al catálogo.

La devolución de cheques es manual y no gravada. Se identifica por separado
para el libro, sin vincular un cheque ni generar otro movimiento de tesorería.
No gravado contable = otros no gravados + devolución. Si el libro presenta ambas
columnas, «No gravado» muestra solo los otros no gravados, evitando duplicación.

La percepción de Entre Ríos utiliza el catálogo existente de regímenes, con
porcentaje, vigencia, base y mínimo configurados. La alícuota se elige por factura. Si se omite, la percepción es cero.
El comprobante conserva la base, la alícuota y el importe registrados. Una modificación de
porcentaje no debe recalcular comprobantes anteriores.

Registrar reemplaza la confirmación como acción del usuario: valida datos y
vinculaciones al plan de Vía 1 y guarda factura, asiento y deuda en una única
transacción. Un fallo no debe dejar factura, asiento ni deuda parciales.

Caso de aceptación con tasas ilustrativas: netos 100 y 200, IVA 21% y 10,5%,
otros no gravados 10, devolución 20, exento 5, percepción 2% sobre neto gravado
(alícuota seleccionada, sin mínimo). Resultado: neto 300, IVA 42, percepción 6,
no gravado contable 30, total 383. En el libro: 300 + 42 + 10 + 5 + 6 + 20 = 383.
La deuda inicial es 383 y el asiento debe balancear por ese importe.

El alta está en Ventas → Nueva factura. Si existen obligaciones compatibles de
Vía 1, distribuir el importe comercial asociable antes de registrar. En el
mismo módulo, Libro IVA Ventas permite consultar por fechas inclusivas. Cada
comprobante conserva su moneda y los importes usan el signo de su tipo.

La migración FacturacionSimple agrega una columna nullable en ventas para la
composición fiscal histórica. No borra datos. Debe estar instalada en la base
correspondiente antes de ejecutar esta versión; generar o compilar el backend
no la aplica. El cálculo de IIBB usa la base, el mínimo y la vigencia del régimen elegido.

Las facturas nuevas se registran en ARS. La imputación a Vía 1 distribuye
únicamente el neto de las líneas fiscales. IVA, percepción, exento, otros no
gravados y devolución de cheques quedan fuera del importe imputable al plan.
Se seleccionan cuotas de una misma moneda por factura. Para USD, el neto
se divide por la cotización de referencia guardada en el comprobante y se
redondea a dos decimales. La vinculación conserva el importe en moneda del plan.

La cobranza se aplica por el total fiscal en ARS. La cancelación de cuotas
usa la proporción acumulada cobrada sobre el total fiscal y el importe
vinculado al plan, con ajuste de centavos al completar el cobro. Las cuotas
anteriores conservan sus vinculaciones históricas. La factura guarda el
criterio de imputación dentro de su composición fiscal.

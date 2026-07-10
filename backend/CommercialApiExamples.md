# Gestión Comercial de Obras - Ejemplos JSON

## Crear acuerdo comercial
POST /api/acuerdos

```json
{
  "clienteExternoId": "CL-123",
  "obraExternaId": "OB-456",
  "numeroAcuerdo": "AC-2026-001",
  "fechaAcuerdo": "2026-05-24T00:00:00Z",
  "descripcion": "Acuerdo de obra civil fase 1",
  "montoTotal": 150000.00,
  "estado": "Aprobado",
  "viaOperacion": "Via1",
  "observaciones": "Presupuesto aprobado por el cliente",
  "usuarioAlta": "admin"
}
```

## Crear plan de pago
POST /api/acuerdos/{id}/plan-pago

```json
{
  "tieneAnticipo": true,
  "montoAnticipo": 30000.00,
  "cantidadCuotas": 4,
  "fechaPrimerVencimiento": "2026-06-15T00:00:00Z",
  "periodicidad": "Mensual",
  "observaciones": "Plan con anticipo y cuotas mensuales"
}
```

## Aprobar acuerdo comercial
POST /api/acuerdos/{id}/aprobar

No se requiere cuerpo de la petición.

## Registrar pago comercial
POST /api/pagos-comerciales

```json
{
  "clienteExternoId": "CL-123",
  "obraExternaId": "OB-456",
  "acuerdoComercialId": 1,
  "fechaPago": "2026-06-10T00:00:00Z",
  "importeTotal": 30000.00,
  "medioPago": "Transferencia",
  "observaciones": "Anticipo recibido"
}
```

## Aplicar pago a cuotas
POST /api/pagos-comerciales/{id}/aplicar

```json
{
  "aplicaciones": [
    {
      "cuotaComercialId": 1,
      "importeAplicado": 30000.00
    }
  ]
}
```

## Consultar estado comercial de un acuerdo
GET /api/acuerdos/{id}/estado-comercial

## Consultar saldo comercial por cliente
GET /api/clientes/{clienteExternoId}/saldo-comercial

## Consultar saldo comercial por obra
GET /api/obras/{obraExternaId}/saldo-comercial

## Consultar cuotas vencidas
GET /api/cuotas/vencidas

## Consultar cuotas pendientes
GET /api/cuotas/pendientes
```

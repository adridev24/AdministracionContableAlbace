/**
 * @typedef {Object} AplicacionPagoTipo
 * @property {number} cuotaComercialId
 * @property {number} importeAplicado
 */

/**
 * @typedef {Object} PagoComercialTipo
 * @property {number} id
 * @property {string} clienteExternoId
 * @property {string} obraExternaId
 * @property {number} acuerdoComercialId
 * @property {string} fechaPago
 * @property {number} importeTotal
 * @property {string} medioPago
 * @property {string} observaciones
 * @property {string} estado
 * @property {AplicacionPagoTipo[]} aplicaciones
 */

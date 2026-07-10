/**
 * @typedef {Object} AcuerdoTipo
 * @property {number} id
 * @property {string} clienteExternoId
 * @property {string} obraExternaId
 * @property {string} numeroAcuerdo
 * @property {string} descripcion
 * @property {string} estado
 * @property {string} observaciones
 * @property {string} fechaAcuerdo
 * @property {string} fechaAlta
 * @property {AcuerdoViaTipo[]} vias
 * @property {number} montoTotal
 * @property {string|null} viaOperacion Campo de compatibilidad para acuerdos de una sola via.
 */

/**
 * @typedef {Object} AcuerdoViaTipo
 * @property {number} id
 * @property {number} acuerdoComercialId
 * @property {string} viaOperacion
 * @property {string} monedaCodigo
 * @property {number} montoOriginal
 * @property {number} montoActual
 * @property {number} totalPagado
 * @property {number} saldoPendiente
 * @property {string} estado
 */

/**
 * @typedef {Object} EstadoComercialTipo
 * @property {number} acuerdoComercialId
 * @property {number} totalPrometido
 * @property {number} totalPagado
 * @property {number} saldoRestante
 */

using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.DTOs.Sales
{
    public class TipoComprobanteVentaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? Letra { get; set; }
        public string TipoFiscal { get; set; } = null!;
        public bool EsCreditoElectronica { get; set; }
        public bool EsExportacion { get; set; }
        public bool RequiereNomenclador { get; set; }
        public bool PermiteIva { get; set; }
        public int Signo { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class PuntoVentaResponse
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; }
        public string? Observaciones { get; set; }
        public List<PuntoVentaComprobanteResponse> ComprobantesPermitidos { get; set; } = new();
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class PuntoVentaComprobanteResponse
    {
        public int Id { get; set; }
        public int PuntoVentaId { get; set; }
        public int PuntoVentaNumero { get; set; }
        public string PuntoVentaDescripcion { get; set; } = null!;
        public int TipoComprobanteVentaId { get; set; }
        public string TipoComprobanteCodigo { get; set; } = null!;
        public string TipoComprobanteDescripcion { get; set; } = null!;
        public bool Activo { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class PuntoVentaSelectorResponse
    {
        public int PuntoVentaComprobanteId { get; set; }
        public int PuntoVentaId { get; set; }
        public int Numero { get; set; }
        public string Descripcion { get; set; } = null!;
        public string TextoMostrar { get; set; } = null!;
        public bool Habilitado { get; set; }
    }

    public class VentaResponse
    {
        public int Id { get; set; }
        public int TipoComprobanteVentaId { get; set; }
        public int? PuntoVentaComprobanteId { get; set; }
        public string TipoComprobanteCodigo { get; set; } = null!;
        public string TipoComprobanteDescripcion { get; set; } = null!;
        public string? TipoComprobanteLetra { get; set; }
        public bool TipoComprobanteEsCreditoElectronica { get; set; }
        public bool TipoComprobanteEsExportacion { get; set; }
        public bool TipoComprobanteRequiereNomenclador { get; set; }
        public bool TipoComprobantePermiteIva { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string? ClienteNombre { get; set; }
        public string ObraExternaId { get; set; } = null!;
        public string? ObraNombre { get; set; }
        public DateTime FechaComprobante { get; set; }
        public int PuntoVenta { get; set; }
        public long NumeroComprobante { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public decimal Cotizacion { get; set; }
        public decimal SubtotalBruto { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal NetoGravado { get; set; }
        public decimal TotalExento { get; set; }
        public decimal TotalNoGravado { get; set; }
        public decimal TotalIva { get; set; }
        public decimal TotalAntesPercepciones { get; set; }
        public decimal TotalPercepciones { get; set; }
        public decimal Total { get; set; }
        public bool PercepcionIibbRequiereRecalculo { get; set; }
        public DateTime? FechaUltimoCalculoPercepcion { get; set; }
        public VentaEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public string? UsuarioConfirmacion { get; set; }
        public int? AsientoContableId { get; set; }
        public List<VentaDetalleResponse> Detalles { get; set; } = new();
        public List<VentaPercepcionIibbResponse> PercepcionesIibb { get; set; } = new();
    }

    public class VentaConfirmacionValidacionResponse
    {
        public bool EsValida { get; set; }
        public List<string> Errores { get; set; } = new();
        public List<string> Advertencias { get; set; } = new();
        public decimal TotalFinal { get; set; }
        public decimal ImporteAsociadoPlan { get; set; }
        public int CantidadObligacionesAplicadas { get; set; }
        public string CodigoOperacionContable { get; set; } = string.Empty;
        public List<string> ConceptosContables { get; set; } = new();
    }

    public class VentaConfirmacionResponse
    {
        public VentaResponse Venta { get; set; } = null!;
        public int AsientoContableId { get; set; }
        public bool AsientoYaExistia { get; set; }
        public decimal TotalFinal { get; set; }
        public decimal ImporteAsociadoPlan { get; set; }
        public int CantidadObligacionesAplicadas { get; set; }
        public string CodigoOperacionContable { get; set; } = string.Empty;
    }

    public class VentaListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<VentaResponse> Items { get; set; } = new();
    }

    public class VentaDetalleResponse
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int NumeroLinea { get; set; }
        public int? ItemFacturableId { get; set; }
        public string? CodigoItem { get; set; }
        public string? ItemFacturableDescripcion { get; set; }
        public int? CategoriaItemFacturableId { get; set; }
        public string? CategoriaItemFacturableCodigo { get; set; }
        public string? CategoriaItemFacturableDescripcion { get; set; }
        public int? UnidadMedidaVentaId { get; set; }
        public string? UnidadMedidaCodigo { get; set; }
        public string? UnidadMedidaDescripcion { get; set; }
        public string? UnidadMedidaAbreviatura { get; set; }
        public string Descripcion { get; set; } = null!;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal ImporteBruto { get; set; }
        public decimal ImporteDescuento { get; set; }
        public decimal Neto { get; set; }
        public int TratamientoIvaId { get; set; }
        public string TratamientoIvaCodigo { get; set; } = null!;
        public string TratamientoIvaDescripcion { get; set; } = null!;
        public TipoTratamientoIvaVenta TipoTratamientoIva { get; set; }
        public decimal PorcentajeIvaAplicado { get; set; }
        public decimal ImporteIva { get; set; }
        public int? NomencladorId { get; set; }
        public string? NomencladorCodigo { get; set; }
        public string? NomencladorDescripcion { get; set; }
        public decimal TotalLinea { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class VentaDetalleMutationResponse
    {
        public VentaDetalleResponse? Detalle { get; set; }
        public VentaResponse Venta { get; set; } = null!;
    }

    public class ClientePercepcionIibbConfigResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public SituacionPercepcionIibbCliente Situacion { get; set; }
        public int? RegimenPercepcionIibbId { get; set; }
        public string? RegimenCodigo { get; set; }
        public string? RegimenDescripcion { get; set; }
        public string? NumeroInscripcionIibb { get; set; }
        public string? JurisdiccionIibb { get; set; }
        public DateTime? ExclusionDesde { get; set; }
        public DateTime? ExclusionHasta { get; set; }
        public string? MotivoExclusion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class VentaPercepcionIibbResponse
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public int? RegimenPercepcionIibbId { get; set; }
        public string? CodigoRegimenAplicado { get; set; }
        public string? DescripcionRegimenAplicada { get; set; }
        public string? JurisdiccionAplicada { get; set; }
        public string? TipoTributoAplicado { get; set; }
        public string? NumeroRegimenAplicado { get; set; }
        public TipoBaseCalculoPercepcionIibb? TipoBaseCalculo { get; set; }
        public decimal BaseImponible { get; set; }
        public decimal AlicuotaAplicada { get; set; }
        public decimal Importe { get; set; }
        public DateTime? VigenciaDesdeAplicada { get; set; }
        public DateTime? VigenciaHastaAplicada { get; set; }
        public ResultadoPercepcionIibb Resultado { get; set; }
        public string? Motivo { get; set; }
        public bool Activa { get; set; }
        public bool EsAutomatica { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class VentaPercepcionIibbCalculoResponse
    {
        public VentaPercepcionIibbResponse? Percepcion { get; set; }
        public ClientePercepcionIibbConfigResponse? ConfiguracionCliente { get; set; }
        public VentaResponse? Venta { get; set; }
    }

    public class AlicuotaIvaVentaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public TipoTratamientoIvaVenta TipoTratamiento { get; set; }
        public decimal Porcentaje { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class NomencladorFceResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class PercepcionIibbEntreRiosResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string Jurisdiccion { get; set; } = null!;
        public string TipoTributo { get; set; } = null!;
        public string NumeroRegimen { get; set; } = null!;
        public decimal Porcentaje { get; set; }
        public TipoBaseCalculoPercepcionIibb TipoBaseCalculo { get; set; }
        public decimal? MontoMinimo { get; set; }
        public DateTime VigenciaDesde { get; set; }
        public DateTime? VigenciaHasta { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class CategoriaItemFacturableResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class UnidadMedidaVentaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? Abreviatura { get; set; }
        public bool PermiteDecimales { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class ItemFacturableResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? DescripcionAmpliada { get; set; }
        public int? CategoriaItemFacturableId { get; set; }
        public string? CategoriaCodigo { get; set; }
        public string? CategoriaDescripcion { get; set; }
        public int UnidadMedidaVentaId { get; set; }
        public string UnidadMedidaCodigo { get; set; } = null!;
        public string UnidadMedidaDescripcion { get; set; } = null!;
        public string? UnidadMedidaAbreviatura { get; set; }
        public int TratamientoIvaPredeterminadoId { get; set; }
        public string TratamientoIvaCodigo { get; set; } = null!;
        public string TratamientoIvaDescripcion { get; set; } = null!;
        public int? NomencladorPredeterminadoId { get; set; }
        public string? NomencladorCodigo { get; set; }
        public string? NomencladorDescripcion { get; set; }
        public decimal? PrecioPredeterminado { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}

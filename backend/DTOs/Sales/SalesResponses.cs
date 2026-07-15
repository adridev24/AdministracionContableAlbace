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
        public VentaEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    public class VentaListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<VentaResponse> Items { get; set; } = new();
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
}

using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.DTOs.Sales
{
    public class TipoComprobanteVentaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public string? Letra { get; set; }
        public int Signo { get; set; }
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class VentaResponse
    {
        public int Id { get; set; }
        public int TipoComprobanteVentaId { get; set; }
        public string TipoComprobanteCodigo { get; set; } = null!;
        public string TipoComprobanteDescripcion { get; set; } = null!;
        public string? TipoComprobanteLetra { get; set; }
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
}

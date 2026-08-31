using BudgetControl.Api.Models.Collections;

namespace BudgetControl.Api.DTOs.Collections
{
    public class MedioPagoCobranzaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string CodigoConceptoContable { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public bool RequiereReferencia { get; set; }
        public bool RequiereBanco { get; set; }
        public bool RequiereFechaValor { get; set; }
        public int Orden { get; set; }
    }

    public class BancoCobranzaResponse
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int Orden { get; set; }
    }

    public class CobranzaListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<CobranzaResponse> Items { get; set; } = new();
    }

    public class CobranzaResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = string.Empty;
        public string? ClienteNombre { get; set; }
        public DateTime Fecha { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public decimal Cotizacion { get; set; }
        public decimal ImporteTotal { get; set; }
        public CobranzaEstado Estado { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = string.Empty;
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public string? UsuarioConfirmacion { get; set; }
        public int? AsientoContableId { get; set; }
        public decimal TotalMedios { get; set; }
        public decimal TotalAplicado { get; set; }
        public int CantidadFacturasAplicadas { get; set; }
        public List<CobranzaMedioPagoResponse> MediosPago { get; set; } = new();
        public List<CobranzaAplicacionFacturaResponse> AplicacionesFactura { get; set; } = new();
    }

    public class CobranzaMedioPagoResponse
    {
        public int Id { get; set; }
        public int CobranzaId { get; set; }
        public int MedioPagoCobranzaId { get; set; }
        public string MedioPagoCodigo { get; set; } = string.Empty;
        public string MedioPagoDescripcion { get; set; } = string.Empty;
        public string CodigoConceptoContable { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public int? BancoCobranzaId { get; set; }
        public string? Banco { get; set; }
        public string? NumeroReferencia { get; set; }
        public DateTime? FechaValor { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CobranzaAplicacionFacturaResponse
    {
        public int Id { get; set; }
        public int CobranzaId { get; set; }
        public int VentaId { get; set; }
        public string Comprobante { get; set; } = string.Empty;
        public DateTime FechaComprobante { get; set; }
        public string ObraExternaId { get; set; } = string.Empty;
        public string? ObraNombre { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal ImporteAplicado { get; set; }
        public decimal CobradoConfirmadoSinEsta { get; set; }
        public decimal ReservadoBorradorSinEsta { get; set; }
        public decimal SaldoDisponibleSinEsta { get; set; }
        public List<CobranzaAplicacionObligacionResponse> AplicacionesObligacion { get; set; } = new();
    }

    public class CobranzaAplicacionObligacionResponse
    {
        public int Id { get; set; }
        public int CobranzaAplicacionFacturaId { get; set; }
        public int CuotaComercialId { get; set; }
        public string TipoObligacion { get; set; } = string.Empty;
        public int NumeroCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal ImporteAplicado { get; set; }
    }

    public class FacturaPendienteCobranzaResponse
    {
        public int VentaId { get; set; }
        public string TipoComprobante { get; set; } = string.Empty;
        public int PuntoVenta { get; set; }
        public long Numero { get; set; }
        public string Comprobante { get; set; } = string.Empty;
        public DateTime FechaComprobante { get; set; }
        public string ClienteExternoId { get; set; } = string.Empty;
        public string ObraExternaId { get; set; } = string.Empty;
        public string? ObraNombre { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal CobradoConfirmado { get; set; }
        public decimal ReservadoBorrador { get; set; }
        public decimal SaldoDisponible { get; set; }
    }

    public class CobranzaConfirmacionResponse
    {
        public CobranzaResponse Cobranza { get; set; } = null!;
        public int AsientoContableId { get; set; }
        public bool AsientoYaExistia { get; set; }
        public string CodigoOperacionContable { get; set; } = string.Empty;
        public List<string> ConceptosContables { get; set; } = new();
        public decimal TotalAplicadoFacturas { get; set; }
        public decimal TotalMediosCancelacion { get; set; }
        public int MovimientoCuentaCorrienteId { get; set; }
    }
}

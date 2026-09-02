namespace BudgetControl.Api.DTOs.Sales
{
    public class CuentaCorrienteClienteFilterRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? Moneda { get; set; }
        public string? ObraId { get; set; }
        public string? EstadoFactura { get; set; }
    }

    public class CuentaCorrienteClienteResponse
    {
        public string ClienteId { get; set; } = string.Empty;
        public string? ClienteNombre { get; set; }
        public List<CuentaCorrienteSaldoMonedaResponse> SaldosPorMoneda { get; set; } = new();
        public List<CuentaCorrienteMovimientoResponse> Movimientos { get; set; } = new();
        public List<CuentaCorrienteFacturaResponse> Facturas { get; set; } = new();
    }

    public class CuentaCorrienteSaldoMonedaResponse
    {
        public string MonedaCodigo { get; set; } = string.Empty;
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal Saldo { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal DebePeriodo { get; set; }
        public decimal HaberPeriodo { get; set; }
        public decimal SaldoFinal { get; set; }
    }

    public class CuentaCorrienteMovimientoResponse
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Debe { get; set; }
        public decimal Haber { get; set; }
        public decimal SaldoAcumulado { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public string ObraId { get; set; } = string.Empty;
        public string? ObraNombre { get; set; }
        public string ModuloOrigen { get; set; } = string.Empty;
        public string IdOrigen { get; set; } = string.Empty;
        public string? NumeroComprobante { get; set; }
    }

    public class CuentaCorrienteFacturaResponse
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoComprobante { get; set; } = string.Empty;
        public string NumeroComprobante { get; set; } = string.Empty;
        public string ObraId { get; set; } = string.Empty;
        public string? ObraNombre { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public decimal TotalFactura { get; set; }
        public decimal TotalCobrado { get; set; }
        public decimal Saldo { get; set; }
        public string EstadoCobranza { get; set; } = string.Empty;
        public List<CuentaCorrienteCobranzaAplicadaResponse> Cobranzas { get; set; } = new();
    }

    public class CuentaCorrienteCobranzaAplicadaResponse
    {
        public int CobranzaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal ImporteAplicado { get; set; }
        public string EstadoCobranza { get; set; } = string.Empty;
    }
}

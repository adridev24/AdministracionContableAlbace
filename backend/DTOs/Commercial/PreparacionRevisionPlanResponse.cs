using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial;

public sealed class PreparacionRevisionPlanResponse
{
    public int AcuerdoComercialId { get; set; }
    public int AcuerdoComercialViaId { get; set; }
    public int PlanPagoId { get; set; }
    public bool TieneAnticipo { get; set; }
    public decimal MontoAnticipo { get; set; }
    public int CantidadCuotas { get; set; }
    public DateTime FechaPrimerVencimiento { get; set; }
    public string Periodicidad { get; set; } = "";
    public AcuerdoEstado EstadoAcuerdo { get; set; }
    public AcuerdoEstado EstadoVia { get; set; }
    public string MonedaCodigo { get; set; } = "";
    public decimal MontoOriginal { get; set; }
    public decimal MontoActual { get; set; }
    public decimal TotalVigente { get; set; }
    public decimal TotalPagadoValido { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal SaldoAFavor { get; set; }
    public uint VersionVia { get; set; }
    public uint VersionPlan { get; set; }
    public int? UltimaCuotaOrdinariaActivaId { get; set; }
    public int SiguienteNumeroCuota { get; set; }
    public List<CuotaPreparacionRevisionResponse> Cuotas { get; set; } = new();
}

public sealed class CuotaPreparacionRevisionResponse
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public TipoCuota Tipo { get; set; }
    public decimal Importe { get; set; }
    public DateTime Vencimiento { get; set; }
    public CuotaEstado Estado { get; set; }
    public uint Version { get; set; }
    public bool TieneHistoria { get; set; }
    public bool PuedeModificar { get; set; }
    public bool PuedeRetirar { get; set; }
    public string? MotivoBloqueoModificacion { get; set; }
    public string? MotivoBloqueoRetiro { get; set; }
}

public sealed class ConfirmacionRevisionPlanResponse
{
    public bool SolicitudYaProcesada { get; set; }
    public RevisionPlanPagoResponse Revision { get; set; } = null!;
    // Current balances, including on a replay; the immutable result is Revision.
    public PreparacionRevisionPlanResponse EstadoActual { get; set; } = null!;
}

using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial;

public enum CambioCuotaRevision { Modificar = 1, Agregar = 2, Retirar = 3 }

public sealed class RevisionPlanPagoRequest
{
    public int AcuerdoComercialId { get; set; }
    public int AcuerdoComercialViaId { get; set; }
    public int PlanPagoId { get; set; }
    public decimal NuevoMontoActual { get; set; }
    public string Comentario { get; set; } = "";
    public Guid SolicitudId { get; set; }
    public bool Confirmado { get; set; }
    public uint VersionVia { get; set; }
    public uint VersionPlan { get; set; }
    public List<VersionCuotaRevision> CuotasOriginales { get; set; } = new();
    public List<CambioCuotaRevisionRequest> Cambios { get; set; } = new();
}

public sealed class VersionCuotaRevision
{
    public int CuotaId { get; set; }
    public uint Version { get; set; }
}

public sealed class CambioCuotaRevisionRequest
{
    public CambioCuotaRevision Operacion { get; set; }
    public int? CuotaId { get; set; }
    public uint? Version { get; set; }
    public TipoCuota? Tipo { get; set; }
    public int? Numero { get; set; }
    public decimal? Importe { get; set; }
    public DateTime? Vencimiento { get; set; }
}

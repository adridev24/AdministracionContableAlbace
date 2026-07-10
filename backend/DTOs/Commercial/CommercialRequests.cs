using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial
{
    public class CreateAcuerdoRequest
    {
        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public string NumeroAcuerdo { get; set; } = null!;

        public decimal? MontoTotal { get; set; }

        [Required]
        public DateTime FechaAcuerdo { get; set; }

        public string? Descripcion { get; set; }
        public AcuerdoEstado Estado { get; set; } = AcuerdoEstado.Borrador;
        public ViaOperacion? ViaOperacion { get; set; }
        public string? Observaciones { get; set; }

        public List<CreateAcuerdoViaRequest> Vias { get; set; } = new();
    }

    public class CreateAcuerdoViaRequest
    {
        [Required]
        public ViaOperacion ViaOperacion { get; set; }

        public ModalidadCobro? ModalidadCobro { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        [Range(0.01, double.MaxValue)]
        public decimal MontoOriginal { get; set; }

        public decimal? MontoActual { get; set; }
        public AcuerdoEstado Estado { get; set; } = AcuerdoEstado.Borrador;
        public string? Observaciones { get; set; }
    }

    public class ModificarMontoViaRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal NuevoMonto { get; set; }

        public bool RefinanciarCuotasPendientes { get; set; }

        [Required]
        public string Motivo { get; set; } = null!;

    }

    public class CreatePlanPagoRequest
    {
        [Required]
        public bool TieneAnticipo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoAnticipo { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadCuotas { get; set; }

        [Required]
        public DateTime FechaPrimerVencimiento { get; set; }

        [Required]
        public string Periodicidad { get; set; } = null!;
        public string? Observaciones { get; set; }
    }

    public class UpdatePlanPagoRequest
    {
        [Required]
        public bool TieneAnticipo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoAnticipo { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadCuotas { get; set; }

        [Required]
        public DateTime FechaPrimerVencimiento { get; set; }

        [Required]
        public string Periodicidad { get; set; } = null!;

        public string? Observaciones { get; set; }

        [Required]
        public List<UpdateCuotaRequest> Cuotas { get; set; } = new();
    }

    public class UpdateCuotaRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteOriginal { get; set; }
    }

    public class CreatePagoComercialRequest
    {
        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public int AcuerdoComercialId { get; set; }

        [Required]
        public int AcuerdoComercialViaId { get; set; }

        [Required]
        public string MonedaCodigo { get; set; } = "ARS";

        [Required]
        public DateTime FechaPago { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteTotal { get; set; }

        [Required]
        public string MedioPago { get; set; } = null!;
        public TipoImputacion TipoImputacion { get; set; } = TipoImputacion.SaldoGeneral;
        public string? Observaciones { get; set; }
        public List<AplicacionPagoRequest> Aplicaciones { get; set; } = new();
    }

    public class AplicacionPagoRequest
    {
        public int? CuotaComercialId { get; set; }
        public int? HitoComercialViaId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteAplicado { get; set; }

        public TipoImputacion TipoImputacion { get; set; } = TipoImputacion.Cuota;
        public string? Observaciones { get; set; }
    }

    public class AjusteCuotaRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal? NuevoImporteOriginal { get; set; }

        public DateTime? NuevaFechaVencimiento { get; set; }

        [Required]
        public string Motivo { get; set; } = null!;

    }

    public class AddCuotaAjusteRequest
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteOriginal { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Required]
        public TipoCuota TipoCuota { get; set; }

        [Required]
        public string Motivo { get; set; } = null!;

    }

    public class AplicarPagoRequest
    {
        [Required]
        public List<AplicacionPagoRequest> Aplicaciones { get; set; } = new();
    }

    public class CreateHitoComercialRequest
    {
        [Required]
        public string Descripcion { get; set; } = null!;

        [Range(0, double.MaxValue)]
        public decimal ImporteEstimado { get; set; }

        [Required]
        public DateTime FechaReferencia { get; set; }

        public string? Observaciones { get; set; }
    }
}

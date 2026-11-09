using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Fiscal;

// Información complementaria: no altera las entidades ni los circuitos operativos.
public class EmpresaFiscal
{
    public int Id { get; set; } = 1;
    [Required, MaxLength(200)] public string RazonSocial { get; set; } = "";
    [Required, MaxLength(11)] public string Cuit { get; set; } = "";
    [Required, MaxLength(200)] public string DomicilioFiscal { get; set; } = "";
    [Required, MaxLength(40)] public string CondicionIva { get; set; } = "Responsable inscripto";
    [MaxLength(40)] public string InscripcionIibb { get; set; } = "";
    [MaxLength(200)] public string Actividad { get; set; } = "";
    public DateTime? InicioActividades { get; set; }
    public DateTime ActualizadoEl { get; set; }
    public string ActualizadoPor { get; set; } = "";
}

public class VentaFiscal
{
    public int VentaId { get; set; }
    public int TipoComprobanteArca { get; set; }
    public int TipoDocumento { get; set; } = 80;
    [Required, MaxLength(20)] public string Documento { get; set; } = "";
    [Required, MaxLength(200)] public string RazonSocial { get; set; } = "";
    [Required, MaxLength(40)] public string CondicionIva { get; set; } = "";
    [MaxLength(1)] public string CodigoOperacion { get; set; } = "";
    public bool DevolucionChequeRevisada { get; set; }
    public DateTime ActualizadoEl { get; set; }
    public string ActualizadoPor { get; set; } = "";
}

public class RetencionFiscal
{
    public int CobranzaMedioPagoId { get; set; }
    [Required, MaxLength(11)] public string CuitAgente { get; set; } = "";
    [Required, MaxLength(200)] public string RazonSocialAgente { get; set; } = "";
    [Required, MaxLength(50)] public string Certificado { get; set; } = "";
    [Required, MaxLength(20)] public string Regimen { get; set; } = "";
    [MaxLength(50)] public string Jurisdiccion { get; set; } = "";
    public DateTime FechaRetencion { get; set; }
    [Required, MaxLength(7)] public string PeriodoComputo { get; set; } = "";
    public DateTime ActualizadoEl { get; set; }
    public string ActualizadoPor { get; set; } = "";
}

public class ExportacionFiscal
{
    public long Id { get; set; }
    [MaxLength(7)] public string Periodo { get; set; } = "";
    public DateTime GeneradoEl { get; set; }
    public string GeneradoPor { get; set; } = "";
    public string NombreArchivo { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public int CantidadComprobantes { get; set; }
    public byte[] Contenido { get; set; } = [];
}

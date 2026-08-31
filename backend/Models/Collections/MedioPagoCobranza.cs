using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Collections
{
    public class MedioPagoCobranza
    {
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = null!;

        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        public string CodigoConceptoContable { get; set; } = null!;

        public bool Activo { get; set; } = true;
        public bool RequiereReferencia { get; set; }
        public bool RequiereBanco { get; set; }
        public bool RequiereFechaValor { get; set; }
        public int Orden { get; set; }
        public DateTime FechaAlta { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;

        public ICollection<CobranzaMedioPago> CobranzasMediosPago { get; set; } = new List<CobranzaMedioPago>();
    }
}

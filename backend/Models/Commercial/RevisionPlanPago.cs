namespace BudgetControl.Api.Models.Commercial
{
    public class RevisionPlanPago
    {
        public int Id { get; set; }
        public Guid SolicitudId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public int AcuerdoComercialViaId { get; set; }
        public int PlanPagoId { get; set; }
        public decimal MontoAnterior { get; set; }
        public decimal MontoNuevo { get; set; }
        public decimal Diferencia { get; set; }
        public string Comentario { get; set; } = null!;
        // Snapshots from authenticated claims; not cascading user references.
        public string UsuarioId { get; set; } = null!;
        public string Usuario { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public TipoRevisionPlanPago TipoRevision { get; set; }
        public string MonedaCodigo { get; set; } = null!;
        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
        public AcuerdoComercialVia AcuerdoComercialVia { get; set; } = null!;
        public PlanPago PlanPago { get; set; } = null!;
        public ICollection<RevisionPlanPagoDetalle> Detalles { get; set; } = new List<RevisionPlanPagoDetalle>();
    }
}

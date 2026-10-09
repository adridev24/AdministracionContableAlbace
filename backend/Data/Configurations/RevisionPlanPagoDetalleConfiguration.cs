using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetControl.Api.Data.Configurations
{
    public class RevisionPlanPagoDetalleConfiguration : IEntityTypeConfiguration<RevisionPlanPagoDetalle>
    {
        public void Configure(EntityTypeBuilder<RevisionPlanPagoDetalle> entity)
        {
            entity.ToTable("revisiones_planes_pago_detalles", table =>
            {
                table.HasCheckConstraint("ck_revision_detalle_identidad", "cuota_original_id > 0 AND numero_cuota >= 0 AND (cuota_comercial_id IS NULL OR cuota_comercial_id = cuota_original_id)");
                table.HasCheckConstraint("ck_revision_detalle_tipo", "tipo_cuota IN (0, 1, 2, 3, 4) AND operacion IN (1, 2, 3, 4)");
                table.HasCheckConstraint("ck_revision_detalle_estados", "(estado_anterior IS NULL OR estado_anterior IN (0, 1, 2, 3, 4)) AND (estado_nuevo IS NULL OR estado_nuevo IN (0, 1, 2, 3, 4))");
                table.HasCheckConstraint("ck_revision_detalle_importes", "(importe_anterior IS NULL OR importe_anterior >= 0) AND (importe_nuevo IS NULL OR importe_nuevo >= 0) AND (importe_pagado_anterior IS NULL OR importe_pagado_anterior >= 0) AND (saldo_pendiente_anterior IS NULL OR saldo_pendiente_anterior >= 0)");
                table.HasCheckConstraint("ck_revision_detalle_valores", @"
                    (operacion = 2 AND cuota_comercial_id IS NOT NULL AND importe_anterior IS NULL AND vencimiento_anterior IS NULL AND estado_anterior IS NULL AND importe_pagado_anterior IS NULL AND saldo_pendiente_anterior IS NULL AND importe_nuevo IS NOT NULL AND vencimiento_nuevo IS NOT NULL AND estado_nuevo IS NOT NULL)
                    OR (operacion = 3 AND cuota_comercial_id IS NULL AND importe_anterior IS NOT NULL AND vencimiento_anterior IS NOT NULL AND estado_anterior IS NOT NULL AND importe_pagado_anterior IS NOT NULL AND importe_pagado_anterior = 0 AND saldo_pendiente_anterior IS NOT NULL AND importe_nuevo IS NULL AND vencimiento_nuevo IS NULL AND estado_nuevo IS NULL)
                    OR (operacion IN (1, 4) AND cuota_comercial_id IS NOT NULL AND importe_anterior IS NOT NULL AND vencimiento_anterior IS NOT NULL AND estado_anterior IS NOT NULL AND importe_pagado_anterior IS NOT NULL AND saldo_pendiente_anterior IS NOT NULL AND importe_nuevo IS NOT NULL AND vencimiento_nuevo IS NOT NULL AND estado_nuevo IS NOT NULL AND (operacion <> 4 OR estado_nuevo = 4))");
            });
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RevisionPlanPagoId).HasColumnName("revision_plan_pago_id");
            entity.Property(e => e.PlanPagoId).HasColumnName("plan_pago_id");
            entity.Property(e => e.CuotaComercialId).HasColumnName("cuota_comercial_id");
            entity.Property(e => e.CuotaOriginalId).HasColumnName("cuota_original_id");
            entity.Property(e => e.NumeroCuota).HasColumnName("numero_cuota");
            entity.Property(e => e.TipoCuota).HasColumnName("tipo_cuota");
            entity.Property(e => e.Operacion).HasColumnName("operacion");
            entity.Property(e => e.ImporteAnterior).HasColumnName("importe_anterior");
            entity.Property(e => e.ImporteNuevo).HasColumnName("importe_nuevo");
            entity.Property(e => e.VencimientoAnterior).HasColumnName("vencimiento_anterior");
            entity.Property(e => e.VencimientoNuevo).HasColumnName("vencimiento_nuevo");
            entity.Property(e => e.EstadoAnterior).HasColumnName("estado_anterior");
            entity.Property(e => e.EstadoNuevo).HasColumnName("estado_nuevo");
            entity.Property(e => e.ImportePagadoAnterior).HasColumnName("importe_pagado_anterior");
            entity.Property(e => e.SaldoPendienteAnterior).HasColumnName("saldo_pendiente_anterior");
            entity.HasIndex(e => new { e.RevisionPlanPagoId, e.CuotaOriginalId }).IsUnique();
            entity.HasIndex(e => e.CuotaOriginalId);
            entity.HasOne(e => e.RevisionPlanPago).WithMany(e => e.Detalles)
                .HasForeignKey(e => new { e.RevisionPlanPagoId, e.PlanPagoId })
                .HasPrincipalKey(e => new { e.Id, e.PlanPagoId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CuotaComercial).WithMany()
                .HasForeignKey(e => new { e.CuotaComercialId, e.PlanPagoId })
                .HasPrincipalKey(e => new { e.Id, e.PlanPagoId }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

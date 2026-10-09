using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetControl.Api.Data.Configurations
{
    public class RevisionPlanPagoConfiguration : IEntityTypeConfiguration<RevisionPlanPago>
    {
        public void Configure(EntityTypeBuilder<RevisionPlanPago> entity)
        {
            entity.ToTable("revisiones_planes_pago", table =>
            {
                table.HasCheckConstraint("ck_revision_montos", "monto_anterior >= 0 AND monto_nuevo >= 0 AND diferencia = monto_nuevo - monto_anterior");
                table.HasCheckConstraint("ck_revision_comentario", "length(btrim(comentario)) > 0");
                table.HasCheckConstraint("ck_revision_usuario", "length(btrim(usuario_id)) > 0 AND length(btrim(usuario)) > 0");
                table.HasCheckConstraint("ck_revision_moneda", "length(btrim(moneda_codigo)) > 0");
                table.HasCheckConstraint("ck_revision_tipo", "tipo_revision IN (1, 2, 3, 4)");
                table.HasCheckConstraint("ck_revision_solicitud", "solicitud_id <> '00000000-0000-0000-0000-000000000000'::uuid");
            });
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SolicitudId).HasColumnName("solicitud_id");
            entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
            entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
            entity.Property(e => e.PlanPagoId).HasColumnName("plan_pago_id");
            entity.Property(e => e.MontoAnterior).HasColumnName("monto_anterior");
            entity.Property(e => e.MontoNuevo).HasColumnName("monto_nuevo");
            entity.Property(e => e.Diferencia).HasColumnName("diferencia");
            entity.Property(e => e.Comentario).HasColumnName("comentario").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Usuario).HasColumnName("usuario").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.TipoRevision).HasColumnName("tipo_revision");
            entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasMaxLength(10).IsRequired();
            entity.HasIndex(e => e.SolicitudId).IsUnique();
            entity.HasIndex(e => new { e.PlanPagoId, e.Fecha });
            entity.HasIndex(e => new { e.AcuerdoComercialViaId, e.Fecha });
            entity.HasIndex(e => new { e.AcuerdoComercialId, e.Fecha });
            entity.HasAlternateKey(e => new { e.Id, e.PlanPagoId });
            entity.HasOne(e => e.AcuerdoComercial).WithMany()
                .HasForeignKey(e => e.AcuerdoComercialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcuerdoComercialVia).WithMany()
                .HasForeignKey(e => new { e.AcuerdoComercialViaId, e.AcuerdoComercialId })
                .HasPrincipalKey(e => new { e.Id, e.AcuerdoComercialId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PlanPago).WithMany()
                .HasForeignKey(e => new { e.PlanPagoId, e.AcuerdoComercialViaId })
                .HasPrincipalKey(e => new { e.Id, e.AcuerdoComercialViaId }).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

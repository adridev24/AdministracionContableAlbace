using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Data.Configurations
{
    public static class PlanRevisionConcurrencyConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            // Npgsql maps uint row versions to PostgreSQL's existing xmin system column.
            modelBuilder.Entity<AcuerdoComercialVia>().Property(e => e.Version).IsRowVersion();
            modelBuilder.Entity<PlanPago>().Property(e => e.Version).IsRowVersion();
            modelBuilder.Entity<CuotaComercial>().Property(e => e.Version).IsRowVersion();
            modelBuilder.Entity<CuotaComercial>().HasIndex(e => new { e.PlanPagoId, e.NumeroCuota }).IsUnique();
            modelBuilder.Entity<CuotaComercial>().HasIndex(e => e.PlanPagoId, "ix_cuota_anticipo_activo")
                .IsUnique().HasFilter("tipo_cuota = 0 AND estado <> 4");

            modelBuilder.Entity<AplicacionPagoComercial>().HasOne(e => e.CuotaComercial)
                .WithMany(e => e.Aplicaciones).HasForeignKey(e => e.CuotaComercialId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VinculacionFacturaComercial>().HasOne(e => e.CuotaComercial)
                .WithMany(e => e.VinculacionesFactura).HasForeignKey(e => e.CuotaComercialId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AjusteCuotaComercial>().HasOne(e => e.CuotaComercial)
                .WithMany(e => e.Ajustes).HasForeignKey(e => e.CuotaComercialId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

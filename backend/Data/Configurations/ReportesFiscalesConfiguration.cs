using BudgetControl.Api.Models.Fiscal;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Collections;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Data.Configurations;

public static class ReportesFiscalesConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<EmpresaFiscal>(e => {
            e.ToTable("reportes_empresa_fiscal", t => t.HasCheckConstraint("CK_reportes_empresa_unica", "\"Id\" = 1"));
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.ActualizadoEl).IsConcurrencyToken();
        });
        model.Entity<VentaFiscal>(e => {
            e.ToTable("reportes_ventas_fiscales");
            e.HasKey(x => x.VentaId);
            e.Property(x => x.ActualizadoEl).IsConcurrencyToken();
            e.HasOne<Venta>().WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<RetencionFiscal>(e => {
            e.ToTable("reportes_retenciones_fiscales");
            e.HasKey(x => x.CobranzaMedioPagoId);
            e.Property(x => x.ActualizadoEl).IsConcurrencyToken();
            e.HasOne<CobranzaMedioPago>().WithMany().HasForeignKey(x => x.CobranzaMedioPagoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.PeriodoComputo);
        });
        model.Entity<ExportacionFiscal>(e => {
            e.ToTable("reportes_exportaciones_fiscales");
            e.HasIndex(x => new { x.Periodo, x.GeneradoEl });
        });
    }
}

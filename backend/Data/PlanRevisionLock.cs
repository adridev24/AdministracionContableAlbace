using System.Data;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BudgetControl.Api.Data
{
    /// <summary>
    /// Infrastructure for a future revision command, not an editing operation.
    /// Use a fresh context and a ReadCommitted transaction, then read dependencies
    /// and validate expected xmin values AFTER acquiring these locks. Keep the same
    /// transaction through validation, audit and SaveChanges. Never release between them.
    /// </summary>
    public static class PlanRevisionLock
    {
        public static async Task<PlanPago> AcquireAsync(AppDbContext db, int viaId, CancellationToken cancellationToken = default)
        {
            if (db.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != IsolationLevel.ReadCommitted)
                throw new InvalidOperationException("La revisión requiere una transacción ReadCommitted explícita.");
            if (db.ChangeTracker.Entries().Any())
                throw new InvalidOperationException("La revisión requiere un contexto sin entidades previamente cargadas.");

            var via = await db.AcuerdosComercialesVias.FromSqlInterpolated(
                $"SELECT v.*, v.xmin FROM acuerdos_comerciales_vias v WHERE v.id = {viaId} FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Vía comercial no encontrada.");
            var plan = await db.PlanesPago.FromSqlInterpolated(
                $"SELECT p.*, p.xmin FROM planes_pago p WHERE p.acuerdo_comercial_via_id = {viaId} FOR UPDATE")
                .AsTracking().SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("La vía no tiene plan de pago.");
            await db.CuotasComerciales.FromSqlInterpolated(
                $"SELECT c.*, c.xmin FROM cuotas_comerciales c WHERE c.plan_pago_id = {plan.Id} ORDER BY c.id FOR UPDATE")
                .AsTracking().LoadAsync(cancellationToken);
            // Fixup populates via.PlanPago and plan.Cuotas. Locks also conflict with FK
            // key-share locks for new payments, installments and direct dependencies.
            return plan;
        }
    }
}

using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Npgsql;

static class RevisionTests
{
    public static async Task RunAsync(Func<AppDbContext> create, NpgsqlConnection connection, Action<bool, string> check)
    {
        async Task Sql(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        async Task RejectSql(string sql, string code, string name)
        {
            try { await Sql(sql); }
            catch (PostgresException ex) when (ex.SqlState == code || (code == "23503" && ex.SqlState == "23001"))
            { check(true, name); return; } // PostgreSQL 18 distinguishes RESTRICT from a missing FK target.
            throw new Exception("Expected SQL rejection: " + name);
        }
        await using var db = create();
        var plan = Fixtures.NewPlan();
        var other = Fixtures.NewPlan();
        db.PlanesPago.AddRange(plan, other);
        await db.SaveChangesAsync();
        var cuotaId = plan.Cuotas.Single().Id;
        var originalAmount = plan.AcuerdoComercialVia.MontoOriginal;

        async Task RejectRevision(Action<RevisionPlanPago> change, string name, string code = "23514")
        {
            await using var ctx = create();
            var revision = Fixtures.Revision(plan);
            change(revision);
            ctx.RevisionesPlanesPago.Add(revision);
            try { await ctx.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == code)
            { check(true, name); return; }
            throw new Exception("Expected revision rejection: " + name);
        }
        await RejectRevision(r => r.Comentario = "   ", "blank comment rejected in PostgreSQL");
        await RejectRevision(r => r.UsuarioId = " ", "blank authenticated user ID rejected");
        await RejectRevision(r => r.Diferencia = 999, "inconsistent difference rejected");
        await RejectRevision(r => r.TipoRevision = (TipoRevisionPlanPago)99, "undefined revision type rejected");
        await RejectRevision(r => r.SolicitudId = Guid.Empty, "empty idempotency identifier rejected");
        await RejectRevision(r => r.AcuerdoComercialId = other.AcuerdoComercialVia.AcuerdoComercialId, "agreement and via must belong together", "23503");
        await RejectRevision(r => r.PlanPagoId = other.Id, "plan and via must belong together", "23503");
        await RejectRevision(r => { var d = Fixtures.Detail(plan); d.CuotaComercialId = other.Cuotas.Single().Id; d.CuotaOriginalId = d.CuotaComercialId.Value; r.Detalles.Add(d); }, "live installment must belong to revision plan", "23503");
        await RejectRevision(r => { var d = Fixtures.Detail(plan); d.ImporteAnterior = null; r.Detalles.Add(d); }, "modified installment requires old values");
        await RejectRevision(r => { var d = Fixtures.Detail(plan); d.Operacion = OperacionRevisionPlanPago.Eliminada; d.CuotaComercialId = null; d.ImporteNuevo = null; d.VencimientoNuevo = null; d.EstadoNuevo = null; d.ImportePagadoAnterior = null; r.Detalles.Add(d); }, "deleted installment cannot omit paid snapshot (SQL NULL semantics)");
        await RejectRevision(r => { var d = Fixtures.Detail(plan); d.Operacion = OperacionRevisionPlanPago.Excluida; r.Detalles.Add(d); }, "excluded installment must end annulled");

        var audit = Fixtures.Revision(plan);
        audit.Detalles.Add(Fixtures.Detail(plan));
        db.RevisionesPlanesPago.Add(audit);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var stored = await db.RevisionesPlanesPago.Include(r => r.Detalles).SingleAsync(r => r.Id == audit.Id);
        // EF relationship fixup normalizes a child's PlanPagoId when it is added to
        // revision.Detalles. Exercise the database FK directly to avoid that normalization.
        await RejectSql($"INSERT INTO revisiones_planes_pago_detalles(revision_plan_pago_id,plan_pago_id,cuota_comercial_id,cuota_original_id,numero_cuota,tipo_cuota,operacion,importe_nuevo,vencimiento_nuevo,estado_nuevo) VALUES({audit.Id},{other.Id},{other.Cuotas.Single().Id},{other.Cuotas.Single().Id},1,1,2,100,now(),0)", "23503", "detail cannot reference another plan");
        check(stored.Detalles.Single().CuotaOriginalId == cuotaId && stored.MonedaCodigo == "ARS" && stored.UsuarioId == "1", "audit roundtrip preserves identities, currency and user");
        check(await db.AcuerdosComercialesVias.Where(v => v.Id == plan.AcuerdoComercialViaId).Select(v => v.MontoOriginal).SingleAsync() == originalAmount, "audit infrastructure does not change original amount");
        await RejectRevision(r => r.SolicitudId = audit.SolicitudId, "idempotency is unique", "23505");
        await RejectSql($"UPDATE revisiones_planes_pago SET comentario='alterado' WHERE id={audit.Id}", "23514", "audit header is append-only");
        await RejectSql($"DELETE FROM revisiones_planes_pago_detalles WHERE revision_plan_pago_id={audit.Id}", "23514", "audit details cannot be deleted");
        await RejectSql("TRUNCATE revisiones_planes_pago_detalles", "23514", "audit cannot be truncated");
        await RejectSql($"DELETE FROM cuotas_comerciales WHERE id={cuotaId}", "23503", "audited live installment cannot be deleted");
        await RejectSql($"DELETE FROM planes_pago WHERE id={plan.Id}", "23503", "plan deletion cannot cascade through audit");

        // A deleted free installment is represented only by snapshots, with no dangling live FK.
        var free = Fixtures.NewPlan();
        db.PlanesPago.Add(free);
        await db.SaveChangesAsync();
        var deletedId = free.Cuotas.Single().Id;
        var deletion = Fixtures.Revision(free);
        var snapshot = Fixtures.Detail(free);
        snapshot.CuotaComercialId = null;
        snapshot.Operacion = OperacionRevisionPlanPago.Eliminada;
        snapshot.ImporteNuevo = null; snapshot.VencimientoNuevo = null; snapshot.EstadoNuevo = null;
        deletion.Detalles.Add(snapshot);
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM cuotas_comerciales WHERE id={deletedId}");
            db.RevisionesPlanesPago.Add(deletion);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        db.ChangeTracker.Clear();
        var savedDeletion = await db.RevisionesPlanesPagoDetalles.SingleAsync(d => d.RevisionPlanPagoId == deletion.Id);
        check(savedDeletion.CuotaComercialId == null && savedDeletion.CuotaOriginalId == deletedId && savedDeletion.ImporteAnterior == 100 && savedDeletion.VencimientoAnterior.HasValue, "deleted free installment remains understandable from immutable snapshot");

        // Removing a draft invoice link must never erase the fact that the relation existed.
        var history = Fixtures.NewPlan();
        db.PlanesPago.Add(history);
        await db.SaveChangesAsync();
        var historyId = history.Cuotas.Single().Id;
        db.VinculacionesFacturaComerciales.Add(new VinculacionFacturaComercial { CuotaComercialId = historyId, FacturaExternaId = "draft-test", NumeroFactura = "test", ImporteVinculado = 10, FechaVinculacion = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await Sql($"DELETE FROM vinculaciones_factura_comerciales WHERE cuota_comercial_id={historyId}");
        await RejectSql($"DELETE FROM cuotas_comerciales WHERE id={historyId}", "23503", "formerly linked installment remains protected after draft unlink");
        await RejectSql($"DELETE FROM cuotas_comerciales_dependencias_historicas WHERE cuota_comercial_id={historyId}", "23514", "dependency evidence cannot be erased");
        await RejectSql($"UPDATE cuotas_comerciales SET numero_cuota=1 WHERE id={historyId}; INSERT INTO cuotas_comerciales(plan_pago_id,numero_cuota,tipo_cuota,fecha_vencimiento,importe_original,importe_pagado,saldo_pendiente,estado) VALUES({history.Id},1,1,now(),1,0,1,0)", "23505", "duplicate installment number rejected");
        await Sql($"INSERT INTO cuotas_comerciales(plan_pago_id,numero_cuota,tipo_cuota,fecha_vencimiento,importe_original,importe_pagado,saldo_pendiente,estado) VALUES({history.Id},0,0,now(),1,0,1,0)");
        await RejectSql($"INSERT INTO cuotas_comerciales(plan_pago_id,numero_cuota,tipo_cuota,fecha_vencimiento,importe_original,importe_pagado,saldo_pendiente,estado) VALUES({history.Id},2,0,now(),1,0,1,0)", "23505", "second active advance rejected independently of number");

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var rollback = Fixtures.Revision(history);
            db.RevisionesPlanesPago.Add(rollback);
            await db.SaveChangesAsync();
            await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            check(!await db.RevisionesPlanesPago.AnyAsync(r => r.SolicitudId == rollback.SolicitudId), "transaction rollback removes the whole audit operation");
        }
    }
}

using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Npgsql;

static class ConcurrencyTests
{
    public static async Task RunAsync(Func<AppDbContext> create, string connectionString, Action<bool, string> check)
    {
        await using var seed = create();
        var plan = Fixtures.NewPlan();
        seed.PlanesPago.Add(plan);
        await seed.SaveChangesAsync();
        var cuotaId = plan.Cuotas.Single().Id;
        foreach (var kind in new[] { "via", "plan", "cuota" })
        {
            await using var first = create();
            await using var second = create();
            if (kind == "via")
            {
                var a = await first.AcuerdosComercialesVias.SingleAsync(v => v.Id == plan.AcuerdoComercialViaId);
                var b = await second.AcuerdosComercialesVias.SingleAsync(v => v.Id == a.Id);
                a.Observaciones = "first"; b.Observaciones = "second";
            }
            else if (kind == "plan")
            {
                var a = await first.PlanesPago.SingleAsync(p => p.Id == plan.Id);
                var b = await second.PlanesPago.SingleAsync(p => p.Id == a.Id);
                a.Observaciones = "first"; b.Observaciones = "second";
            }
            else
            {
                var a = await first.CuotasComerciales.SingleAsync(c => c.Id == cuotaId);
                var b = await second.CuotasComerciales.SingleAsync(c => c.Id == cuotaId);
                a.FechaVencimiento = a.FechaVencimiento.AddDays(1); b.FechaVencimiento = b.FechaVencimiento.AddDays(2);
            }
            await first.SaveChangesAsync();
            try { await second.SaveChangesAsync(); throw new Exception("Expected concurrency conflict: " + kind); }
            catch (DbUpdateConcurrencyException) { check(true, kind + " rejects stale updates across independent contexts"); }
        }
        await using (var noTransaction = create())
        {
            try { await PlanRevisionLock.AcquireAsync(noTransaction, plan.AcuerdoComercialViaId); throw new Exception("Missing transaction guard"); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("ReadCommitted")) { check(true, "locking requires an explicit transaction"); }
        }

        async Task CheckBlocked(string sql, string name)
        {
            await using var owner = create();
            await using var tx = await owner.Database.BeginTransactionAsync();
            var locked = await PlanRevisionLock.AcquireAsync(owner, plan.AcuerdoComercialViaId);
            check(locked.Cuotas.Any(c => c.Id == cuotaId), "lock loads fresh complete plan: " + name);
            await using var contender = new NpgsqlConnection(connectionString);
            await contender.OpenAsync();
            await using (var settings = new NpgsqlCommand("SET lock_timeout='250ms'", contender)) await settings.ExecuteNonQueryAsync();
            try
            {
                await using var command = new NpgsqlCommand(sql, contender);
                await command.ExecuteNonQueryAsync();
                throw new Exception("Expected lock conflict: " + name);
            }
            catch (PostgresException ex) when (ex.SqlState == "55P03") { check(true, name); }
            await tx.RollbackAsync();
        }
        await CheckBlocked($"UPDATE cuotas_comerciales SET importe_original=99 WHERE id={cuotaId}", "concurrent installment edit waits for revision");
        await CheckBlocked($"INSERT INTO vinculaciones_factura_comerciales(cuota_comercial_id,factura_externa_id,numero_factura,importe_vinculado,fecha_vinculacion) VALUES({cuotaId},'test','test',1,now())", "concurrent invoice link cannot enter validation/save window");
        await CheckBlocked($"INSERT INTO cuotas_comerciales(plan_pago_id,numero_cuota,tipo_cuota,fecha_vencimiento,importe_original,importe_pagado,saldo_pendiente,estado) VALUES({plan.Id},2,1,now(),1,0,1,0)", "new higher installment cannot enter locked plan");
        await CheckBlocked($"SELECT id FROM acuerdos_comerciales_vias WHERE id={plan.AcuerdoComercialViaId} FOR UPDATE", "second revision cannot lock the same aggregate");

        // A movement must not silently resume with old quota values after a revision commits.
        await using (var owner = create())
        await using (var contender = new NpgsqlConnection(connectionString))
        await using (var observer = new NpgsqlConnection(connectionString))
        {
            await using var tx = await owner.Database.BeginTransactionAsync();
            var locked = await PlanRevisionLock.AcquireAsync(owner, plan.AcuerdoComercialViaId);
            locked.Cuotas.Single().FechaVencimiento = locked.Cuotas.Single().FechaVencimiento.AddDays(1);
            await owner.SaveChangesAsync();
            await contender.OpenAsync();
            await observer.OpenAsync();
            await using var insert = new NpgsqlCommand($"INSERT INTO vinculaciones_factura_comerciales(cuota_comercial_id,factura_externa_id,numero_factura,importe_vinculado,fecha_vinculacion) VALUES({cuotaId},'waiting','waiting',1,now())", contender);
            var pending = insert.ExecuteNonQueryAsync();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            var blocked = false;
            while (DateTime.UtcNow < deadline && !pending.IsCompleted)
            {
                await using var probe = new NpgsqlCommand("SELECT cardinality(pg_blocking_pids(@pid)) > 0", observer);
                probe.Parameters.AddWithValue("pid", contender.ProcessID);
                blocked = (bool)(await probe.ExecuteScalarAsync())!;
                if (blocked) break;
                await Task.Delay(20);
            }
            check(blocked, "movement waits while revision changes the installment");
            await tx.CommitAsync();
            try { await pending; throw new Exception("Expected serialization failure after quota change"); }
            catch (PostgresException ex) when (ex.SqlState == "40001")
            { check(true, "waiting movement rejects changed quota instead of using stale validation"); }
            await using var verify = create();
            check(!await verify.VinculacionesFacturaComerciales.AnyAsync(v => v.FacturaExternaId == "waiting"), "rejected concurrent movement leaves no link");
        }

        // Reverse order: a committed new relation is visible after acquiring the locks.
        await using (var movement = create())
        {
            movement.VinculacionesFacturaComerciales.Add(new VinculacionFacturaComercial { CuotaComercialId = cuotaId, FacturaExternaId = "existing", NumeroFactura = "existing", ImporteVinculado = 1, FechaVinculacion = DateTime.UtcNow });
            await movement.SaveChangesAsync();
        }
        await using (var revision = create())
        {
            await using var tx = await revision.Database.BeginTransactionAsync();
            await PlanRevisionLock.AcquireAsync(revision, plan.AcuerdoComercialViaId);
            check(await revision.VinculacionesFacturaComerciales.AnyAsync(v => v.CuotaComercialId == cuotaId), "revision re-read sees relation committed before locking");
            await tx.RollbackAsync();
        }
    }
}

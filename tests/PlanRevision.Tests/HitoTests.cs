using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;

static class HitoTests
{
    public static async Task RunAsync(Func<AppDbContext> create, Action<bool, string> check)
    {
        var plan = await RevisionOperationTests.Seed(create, 2);
        var viaId = plan.AcuerdoComercialViaId;
        var user = new CurrentUserService(RevisionOperationTests.Identity());
        CreateHitoComercialRequest Request(decimal amount = 100) => new()
        { Descripcion = "Hito de prueba", ImporteEstimado = amount, FechaReferencia = DateTime.UtcNow, Observaciones = "Original" };
        async Task<HitoComercialResponse> Edit(int id, decimal amount)
        {
            await using var db = create();
            return await new ComercialService(db, user).ActualizarHitoAsync(viaId, id, Request(amount));
        }
        async Task Delete(int id)
        {
            await using var db = create();
            await new ComercialService(db, user).EliminarHitoAsync(viaId, id);
        }
        async Task Reject(Func<Task> operation, string name)
        {
            try { await operation(); throw new Exception("Expected rejection: " + name); }
            catch (InvalidOperationException) { check(true, name); }
        }
        int hitoId;
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.SingleAsync(v => v.Id == viaId);
            via.ModalidadCobro = ModalidadCobro.Abierta;
            await db.SaveChangesAsync();
            var service = new ComercialService(db, user);
            hitoId = (await service.CrearHitoAsync(viaId, Request())).Id;
        }
        await using (var db = create())
            await Reject(() => new ComercialService(db, user).CrearHitoAsync(viaId, Request(10000)), "hito creation above plan balance rejected");
        await Reject(() => Edit(hitoId, 10000), "hito edit above plan balance rejected");
        await using (var db = create())
            check(await db.HitosComercialesVias.CountAsync(h => h.AcuerdoComercialViaId == viaId) == 1 &&
                (await db.HitosComercialesVias.SingleAsync(h => h.Id == hitoId)).ImporteEstimado == 100,
                "rejected excessive amounts leave hitos unchanged");
        check((await Edit(hitoId, 0)).Estado == HitoEstado.Pendiente, "unpaid hito can be edited to zero");
        await Reject(() => Edit(hitoId, -1), "negative hito amount rejected");
        await Reject(() => Edit(hitoId, 1.001m), "fractional cents rejected");
        await using (var db = create())
            await Reject(() => new ComercialService(db, user).ActualizarHitoAsync(viaId + 1000000, hitoId, Request()), "hito cannot be edited through another via");
        await Delete(hitoId);
        await using (var db = create())
        {
            check(!await db.HitosComercialesVias.AnyAsync(h => h.Id == hitoId), "unpaid hito deleted");
            hitoId = (await new ComercialService(db, user).CrearHitoAsync(viaId, Request())).Id;
        }
        int pagoId;
        await using (var db = create())
        {
            var payment = await new PagoComercialService(db, user).RegistrarPagoAsync(new()
            {
                ClienteExternoId = "test", ObraExternaId = "test", AcuerdoComercialId = plan.AcuerdoComercialVia.AcuerdoComercialId,
                AcuerdoComercialViaId = viaId, MonedaCodigo = "ARS", MedioPago = "Efectivo", FechaPago = DateTime.UtcNow,
                ImporteTotal = 40, TipoImputacion = TipoImputacion.Hito,
                Aplicaciones = new() { new() { HitoComercialViaId = hitoId, TipoImputacion = TipoImputacion.Hito, ImporteAplicado = 40 } }
            });
            pagoId = payment.Id;
        }
        check((await Edit(hitoId, 160)).ImporteEstimado == 160, "hito accepts exact literal remaining balance");
        await Reject(() => Edit(hitoId, 160.01m), "hito rejects one cent above literal balance without adding its paid amount");
        await using (var db = create())
            await Reject(() => new ComercialService(db, user).CrearHitoAsync(viaId, Request(160.01m)), "new hito uses balance after payments");
        await Reject(() => Delete(hitoId), "paid hito deletion blocked");
        await Reject(() => Edit(hitoId, 39.99m), "hito cannot be reduced below applied payments");
        var equal = await Edit(hitoId, 40);
        check(equal.Estado == HitoEstado.Cumplido && equal.ImporteAplicado == 40 && equal.TienePagosAsociados, "equal amount allowed and state fulfilled");
        check((await Edit(hitoId, 80)).Estado == HitoEstado.Parcial, "raising amount recalculates partial state");
        await using (var db = create())
        {
            await db.Database.BeginTransactionAsync();
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM hitos_comerciales_vias WHERE id = {hitoId}");
                throw new Exception("Database allowed deletion of associated hito");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState is "23503" or "23001")
            { check(true, "database restricts deleting associated hito"); }
        }
        // A concurrent edit must wait for payment-side row locks and then re-read applications.
        await using (var locked = create())
        {
            await using var tx = await locked.Database.BeginTransactionAsync();
            await locked.HitosComercialesVias.FromSqlInterpolated($"SELECT * FROM hitos_comerciales_vias WHERE id = {hitoId} FOR UPDATE").LoadAsync();
            var edit = Edit(hitoId, 70);
            await Task.Delay(200);
            check(!edit.IsCompleted, "hito edit waits for concurrent row lock");
            await locked.Database.ExecuteSqlInterpolatedAsync($"UPDATE aplicaciones_pago_comerciales SET importe_aplicado = 75 WHERE hito_comercial_via_id = {hitoId}");
            await locked.Database.ExecuteSqlInterpolatedAsync($"UPDATE hitos_comerciales_vias SET importe_aplicado = 75 WHERE id = {hitoId}");
            await locked.Database.ExecuteSqlInterpolatedAsync($"UPDATE pagos_comerciales SET importe_total = 75 WHERE id = {pagoId}");
            await tx.CommitAsync();
            await Reject(async () => { await edit; }, "edit rechecks committed payment amount after lock");
        }
        await using (var db = create())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE pagos_comerciales SET importe_total = 170 WHERE id = {pagoId}");
        await Reject(() => Edit(hitoId, 75), "editing blocked when literal balance is below payments already assigned");
        await using (var db = create())
            await new PagoComercialService(db, user).AnularPagoAsync(pagoId, new() { Motivo = "Prueba" });
        check((await Edit(hitoId, 200)).ImporteEstimado == 200, "cancelled payment restores full balance for hito limit");
        var cancelled = await Edit(hitoId, 0);
        check(cancelled.ImporteAplicado == 0 && cancelled.Estado == HitoEstado.Pendiente && cancelled.TienePagosAsociados,
            "cancelled payments excluded from minimum but history retained");
        await Reject(() => Delete(hitoId), "cancelled payment history blocks deletion");
    }
}

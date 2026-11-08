using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;

static class MontoAbiertoTests
{
    public static async Task RunAsync(Func<AppDbContext> create, Action<bool, string> check)
    {
        var plan = await RevisionOperationTests.Seed(create, 2);
        var viaId = plan.AcuerdoComercialViaId;
        var user = new CurrentUserService(RevisionOperationTests.Identity());
        async Task<AcuerdoViaResponse> Change(decimal amount, string reason = "Ajuste solicitado", bool refinance = false)
        {
            await using var db = create();
            return await new ComercialService(db, user).ModificarMontoViaAsync(viaId, new()
            { NuevoMonto = amount, Motivo = reason, RefinanciarCuotasPendientes = refinance });
        }
        async Task Reject(Func<Task> action, string description)
        {
            try { await action(); throw new Exception("Expected rejection: " + description); }
            catch (InvalidOperationException) { check(true, description); }
        }
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == viaId);
            via.ModalidadCobro = ModalidadCobro.Abierta;
            via.Estado = via.AcuerdoComercial.Estado = AcuerdoEstado.Aprobado;
            db.PlanesPago.Remove(await db.PlanesPago.SingleAsync(p => p.Id == plan.Id));
            await db.SaveChangesAsync();
        }
        check((await Change(300)).MontoActual == 300, "approved open via amount can increase");
        await Reject(() => Change(300, "  "), "open amount requires reason");
        await Reject(() => Change(1.001m), "open amount rejects fractional cents");
        await Reject(() => Change(0), "open amount must be positive");
        await Reject(() => Change(300, refinance: true), "open amount rejects quota refinancing");
        var ids = new List<int>();
        foreach (var amount in new[] { 60m, 40m })
        {
            await using var db = create();
            var hito = await new ComercialService(db, user).CrearHitoAsync(viaId, new()
            { Descripcion = "Hito " + amount, ImporteEstimado = amount, FechaReferencia = DateTime.UtcNow });
            ids.Add(hito.Id);
        }
        int paymentId;
        await using (var db = create())
        {
            paymentId = (await new PagoComercialService(db, user).RegistrarPagoAsync(new()
            {
                AcuerdoComercialId = plan.AcuerdoComercialVia.AcuerdoComercialId, AcuerdoComercialViaId = viaId,
                ClienteExternoId = "test", ObraExternaId = "test", MonedaCodigo = "ARS", MedioPago = "Efectivo",
                FechaPago = DateTime.UtcNow, ImporteTotal = 50, TipoImputacion = TipoImputacion.Hito,
                Aplicaciones = new() { new() { HitoComercialViaId = ids[0], TipoImputacion = TipoImputacion.Hito, ImporteAplicado = 20 },
                    new() { HitoComercialViaId = ids[1], TipoImputacion = TipoImputacion.Hito, ImporteAplicado = 30 } }
            })).Id;
        }
        await Reject(() => Change(49.99m), "open amount covers payments across multiple hitos");
        await Reject(() => Change(100), "reduced balance must cover existing hito amounts");
        var reduced = await Change(110);
        check(reduced.MontoActual == 110 && reduced.TotalPagado == 50 && reduced.SaldoPendiente == 60 && reduced.Estado == AcuerdoEstado.Aprobado,
            "open amount accepts exact hito balance boundary without changing state");
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == viaId);
            check(via.MontoOriginal == 200 && via.AcuerdoComercial.MontoTotal == 110, "open amount preserves original and updates agreement total");
            var audits = await db.AjustesAcuerdosComercialesVias.Where(a => a.AcuerdoComercialViaId == viaId).OrderBy(a => a.Id).ToListAsync();
            check(audits.Count == 2 && audits.Last().MontoAnterior == 300 && audits.Last().MontoNuevo == 110 &&
                audits.Last().Motivo == "Ajuste solicitado" && audits.Last().UsuarioAjuste == "admin-test",
                "successful open changes audited; rejected changes leave no audit");
            via.Estado = via.AcuerdoComercial.Estado = AcuerdoEstado.EnCurso;
            await db.SaveChangesAsync();
        }
        check((await Change(120)).MontoActual == 120, "in-progress open amount can change");
        await using (var db = create())
            await new PagoComercialService(db, user).AnularPagoAsync(paymentId, new() { Motivo = "Anulacion de prueba" });
        check((await Change(60)).SaldoPendiente == 60, "cancelled payments excluded from open amount floor");
        foreach (var state in new[] { AcuerdoEstado.Anulado, AcuerdoEstado.Finalizado })
        {
            await using (var db = create())
            {
                var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == viaId);
                via.Estado = via.AcuerdoComercial.Estado = state;
                await db.SaveChangesAsync();
            }
            await Reject(() => Change(300), "closed open via cannot change amount: " + state);
        }
        var planned = await RevisionOperationTests.Seed(create, 2);
        await using (var db = create())
            await Reject(() => new ComercialService(db, user).ModificarMontoViaAsync(planned.AcuerdoComercialViaId,
                new() { NuevoMonto = 300, Motivo = "No bypass" }), "approved planned via still requires revision workflow");
    }
}

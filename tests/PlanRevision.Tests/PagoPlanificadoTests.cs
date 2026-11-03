using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;

static class PagoPlanificadoTests
{
    public static async Task RunAsync(Func<AppDbContext> create, Action<bool, string> check)
    {
        var plan = await RevisionOperationTests.Seed(create, 2, true);
        var cuotas = plan.Cuotas.OrderBy(c => c.NumeroCuota).ToList();
        PagoComercialService Service(AppDbContext db) => new(db, new CurrentUserService(RevisionOperationTests.Identity()));
        CreatePagoComercialRequest Request() => new()
        {
            ClienteExternoId = "test", ObraExternaId = "test", AcuerdoComercialId = plan.AcuerdoComercialVia.AcuerdoComercialId,
            AcuerdoComercialViaId = plan.AcuerdoComercialViaId, MonedaCodigo = "ARS", MedioPago = "Efectivo",
            FechaPago = DateTime.UtcNow, ImporteTotal = 50, TipoImputacion = TipoImputacion.Cuota,
            Aplicaciones = new() { new() { CuotaComercialId = cuotas[0].Id, TipoImputacion = TipoImputacion.Cuota, ImporteAplicado = 20 },
                new() { CuotaComercialId = cuotas[1].Id, TipoImputacion = TipoImputacion.Cuota, ImporteAplicado = 30 } }
        };
        foreach (var mode in new[] { "general", "anticipo-suelto", "parcial", "hito", "sin-aplicaciones", "incompleto", "negativo", "saldo-excedido", "cuota-ajena" })
        {
            var request = Request();
            switch (mode)
            {
                case "general": request.TipoImputacion = TipoImputacion.SaldoGeneral; break;
                case "anticipo-suelto": request.TipoImputacion = TipoImputacion.Anticipo; break;
                case "parcial": request.TipoImputacion = TipoImputacion.PagoParcial; break;
                case "hito": request.Aplicaciones[0].TipoImputacion = TipoImputacion.Hito; break;
                case "sin-aplicaciones": request.Aplicaciones.Clear(); break;
                case "incompleto": request.Aplicaciones[0].ImporteAplicado = 10; break;
                case "negativo": request.Aplicaciones[0].ImporteAplicado = -10; request.Aplicaciones[1].ImporteAplicado = 60; break;
                case "saldo-excedido": request.ImporteTotal = 150; request.Aplicaciones[1].ImporteAplicado = 130; break;
                case "cuota-ajena": var other = await RevisionOperationTests.Seed(create, 1); request.Aplicaciones[1].CuotaComercialId = other.Cuotas.Single().Id; break;
            }
            await using (var db = create())
            {
                try { await Service(db).RegistrarPagoAsync(request); throw new Exception("Accepted invalid payment: " + mode); }
                catch (InvalidOperationException) { }
            }
            await using var verify = create();
            check(!await verify.PagosComerciales.AnyAsync(p => p.AcuerdoComercialViaId == plan.AcuerdoComercialViaId) &&
                await verify.CuotasComerciales.Where(c => c.PlanPagoId == plan.Id).AllAsync(c => c.ImportePagado == 0), "planned payment rejects and rolls back: " + mode);
        }
        await using (var db = create()) { await Service(db).RegistrarPagoAsync(Request()); }
        await using (var verify = create())
        {
            var payment = await verify.PagosComerciales.Include(p => p.Aplicaciones).SingleAsync(p => p.AcuerdoComercialViaId == plan.AcuerdoComercialViaId);
            check(payment.Aplicaciones.Count == 2 && payment.Aplicaciones.Sum(a => a.ImporteAplicado) == payment.ImporteTotal && payment.Aplicaciones.All(a => a.CuotaComercialId.HasValue), "planned payment fully allocated across advance and installment");
            check((await verify.CuotasComerciales.FindAsync(cuotas[0].Id))!.ImportePagado == 20 && (await verify.CuotasComerciales.FindAsync(cuotas[1].Id))!.ImportePagado == 30, "partial advance and installment balances updated");
        }
    }
}

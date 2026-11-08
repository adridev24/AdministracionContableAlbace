using BudgetControl.Api.Data;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services.Sales;
using Microsoft.EntityFrameworkCore;

static class AcuerdosAbiertosFacturaTests
{
    public static async Task RunAsync(Func<AppDbContext> create, Action<bool, string> check)
    {
        async Task<int> Seed(string currency = "ARS", ViaOperacion operation = ViaOperacion.Via1,
            ModalidadCobro modality = ModalidadCobro.Abierta, AcuerdoEstado state = AcuerdoEstado.Aprobado,
            string client = "open-client", string project = "open-project", bool hitos = false)
        {
            var plan = await RevisionOperationTests.Seed(create, 2);
            await using var db = create();
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == plan.AcuerdoComercialViaId);
            via.ViaOperacion = operation; via.ModalidadCobro = modality; via.Estado = state; via.MonedaCodigo = currency;
            via.AcuerdoComercial.Estado = state;
            via.AcuerdoComercial.ClienteExternoId = client; via.AcuerdoComercial.ObraExternaId = project;
            if (modality == ModalidadCobro.Abierta) db.PlanesPago.Remove(await db.PlanesPago.SingleAsync(p => p.Id == plan.Id));
            if (hitos) via.Hitos.Add(new() { Descripcion = "Entrega inicial", ImporteEstimado = 70,
                FechaReferencia = DateTime.UtcNow.Date, Estado = HitoEstado.Pendiente, UsuarioAlta = "test", FechaAlta = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return via.Id;
        }
        var empty = await Seed();
        var withHitos = await Seed(hitos: true);
        var usd = await Seed(currency: "USD");
        await Seed(operation: ViaOperacion.Via2);
        var planned = await Seed(modality: ModalidadCobro.Planificada);
        await Seed(state: AcuerdoEstado.Anulado);
        await Seed(state: AcuerdoEstado.Finalizado);
        await Seed(client: "other-client");
        await Seed(project: "other-project");
        var agreementClosed = await Seed();
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == agreementClosed);
            via.AcuerdoComercial.Estado = AcuerdoEstado.Finalizado;
            await db.SaveChangesAsync();
        }
        await using var query = create();
        var service = new VentasService(query, null!, null!, null!, null!, null!, null!);
        var rows = (await service.GetAcuerdosAbiertosVia1Async("open-client", "open-project", "ARS")).ToList();
        check(rows.Select(r => r.ViaId).OrderBy(i => i).SequenceEqual(new[] { empty, withHitos, usd }.OrderBy(i => i)),
            "invoice open agreement lookup filters client, project, modality, via and closed states");
        check(rows.Single(r => r.ViaId == empty).Hitos.Count == 0, "open agreements without hitos or payment plan remain visible");
        var row = rows.Single(r => r.ViaId == withHitos);
        check(row.MontoActual == 200 && row.Hitos.Single().Descripcion == "Entrega inicial" && row.Hitos.Single().ImporteEstimado == 70,
            "open agreement lookup returns agreement amount and real hitos");
        check((await service.GetAcuerdosAbiertosVia1Async("open-client", "open-project", "USD")).Single().ViaId == usd,
            "USD invoice excludes ARS agreements; ARS invoice can show USD agreements");
        check(!(await service.GetAcuerdosAbiertosVia1Async("", "open-project", "ARS")).Any(), "missing client cannot list unrelated agreements");
        var installments = (await service.DisponiblesAsync(new() { ClienteExternoId = "open-client", ObraExternaId = "open-project", MonedaCodigo = "ARS" })).ToList();
        check(installments.Count == 2, "existing planned invoice obligations remain unchanged beside open agreements");
    }
}

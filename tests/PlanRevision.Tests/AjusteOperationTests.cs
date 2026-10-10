using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;

static class AjusteOperationTests
{
    public static async Task RunAsync(Func<AppDbContext> create, HttpClient client, Func<string, string> token,
        JsonSerializerOptions json, Action<bool, string> check)
    {
        var plan = await RevisionOperationTests.Seed(create, 2);
        var route = $"/api/comercial/planes/{plan.Id}/cuotas-ajuste";
        AddCuotaAjusteRequest Request(TipoCuota type = TipoCuota.Adicional) => new()
        { TipoCuota = type, ImporteOriginal = 25, FechaVencimiento = CommercialCalendar.Today.AddMonths(1), Motivo = "Ajuste autorizado" };

        client.DefaultRequestHeaders.Authorization = null;
        check((await client.PostAsJsonAsync(route, Request(), json)).StatusCode == HttpStatusCode.Unauthorized, "adjustment HTTP anonymous = 401");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token("User"));
        check((await client.PostAsJsonAsync(route, Request(), json)).StatusCode == HttpStatusCode.Forbidden, "adjustment HTTP non-Admin = 403, not 409");
        foreach (var role in new string?[] { null, "User" })
        {
            await using var db = create();
            var accessor = RevisionOperationTests.Identity(role);
            var service = new ComercialService(db, new CurrentUserService(accessor), accessor);
            try { await service.AgregarCuotaAjusteAsync(plan.Id, Request()); throw new Exception("Expected service authorization denial"); }
            catch (UnauthorizedAccessException) { check(true, "adjustment service independently rejects " + (role ?? "anonymous")); }
        }
        // No HTTP accessor is also fail-closed, including direct use with an arbitrary IUserContext.
        await using (var db = create())
        {
            var service = new ComercialService(db, new CurrentUserService(RevisionOperationTests.Identity()));
            try { await service.AgregarCuotaAjusteAsync(plan.Id, Request()); throw new Exception("Expected missing-principal denial"); }
            catch (UnauthorizedAccessException) { check(true, "adjustment service does not trust audit-only IUserContext for Admin authorization"); }
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token("Admin"));
        foreach (var type in new[] { TipoCuota.Cuota, TipoCuota.Anticipo, (TipoCuota)99 })
        {
            var response = await client.PostAsJsonAsync(route, Request(type), json);
            check(response.StatusCode == HttpStatusCode.BadRequest, "Admin cannot submit disallowed adjustment type: " + type);
            if (type == TipoCuota.Cuota)
                check((await response.Content.ReadAsStringAsync()).Contains("plan-pago/revision"), "ordinary adjustment rejection identifies revision endpoint");
            await using var db = create();
            var accessor = RevisionOperationTests.Identity();
            try { await new ComercialService(db, new CurrentUserService(accessor), accessor).AgregarCuotaAjusteAsync(plan.Id, Request(type)); throw new Exception("Expected invalid type"); }
            catch (InvalidOperationException) { check(true, "direct service also rejects adjustment type: " + type); }
        }
        foreach (var state in new[] { "Borrador", "Aprobado", "Anulada" })
        {
            var response = await client.PostAsJsonAsync(route, new { tipoCuota = 1, importeOriginal = 25, fechaVencimiento = CommercialCalendar.Today,
                motivo = "Bypass attempt", estado = state, estadoPlan = state, usuario = "forged", role = "Admin" });
            check(response.StatusCode == HttpStatusCode.BadRequest, "numeric ordinary type and forged state cannot bypass: " + state);
        }
        check((await client.PostAsJsonAsync(route, new { importeOriginal = 25, fechaVencimiento = CommercialCalendar.Today, motivo = "missing type" })).StatusCode == HttpStatusCode.BadRequest,
            "omitted type cannot default to an accepted adjustment");
        foreach (var invalid in new[] { "comment", "amount", "precision", "date" })
        {
            var r = Request();
            switch (invalid) { case "comment": r.Motivo = "  "; break; case "amount": r.ImporteOriginal = -1; break;
                case "precision": r.ImporteOriginal = 1.001m; break; case "date": r.FechaVencimiento = default; break; }
            check((await client.PostAsJsonAsync(route, r, json)).StatusCode == HttpStatusCode.BadRequest, "adjustment validates " + invalid);
        }
        await using (var db = create())
            check(await db.CuotasComerciales.CountAsync(c => c.PlanPagoId == plan.Id) == 2 && !await db.AjustesCuotaComerciales.AnyAsync(a => a.PlanPagoId == plan.Id), "denied adjustments leave quotas and audit untouched");

        var expected = 200m;
        foreach (var type in new[] { TipoCuota.Adicional, TipoCuota.Refuerzo, TipoCuota.Ajuste })
        {
            var payload = JsonSerializer.SerializeToNode(Request(type), json)!;
            payload["estado"] = "Anulada"; payload["montoOriginal"] = 1; payload["usuario"] = "forged";
            var response = await client.PostAsJsonAsync(route, payload, json);
            check(response.StatusCode == HttpStatusCode.Created, "Admin adjustment accepts existing type: " + type);
            expected += 25;
            await using var db = create();
            var via = await db.AcuerdosComercialesVias.AsNoTracking().SingleAsync(v => v.Id == plan.AcuerdoComercialViaId);
            var quotas = await db.CuotasComerciales.AsNoTracking().Where(c => c.PlanPagoId == plan.Id).OrderBy(c => c.Id).ToListAsync();
            check(quotas.Last().TipoCuota == type && quotas.Last().Estado == CuotaEstado.Pendiente, "submitted state cannot change server-defined adjustment type/state: " + type);
            check(via.MontoOriginal == 200 && via.MontoActual == expected && quotas.Where(c => c.Estado != CuotaEstado.Anulada).Sum(c => c.ImporteOriginal) == expected,
                "adjustment preserves MontoOriginal and exact active total: " + type);
            var audit = await db.AjustesCuotaComerciales.SingleAsync(a => a.CuotaComercialId == quotas.Last().Id);
            check(audit.UsuarioAjuste == "admin-jwt" && audit.Motivo == "Ajuste autorizado" && audit.TipoAjuste == TipoAjuste.NuevaCuota &&
                await db.AjustesAcuerdosComercialesVias.AnyAsync(a => a.AcuerdoComercialViaId == via.Id && a.MontoNuevo == expected) &&
                !await db.RevisionesPlanesPago.AnyAsync(r => r.PlanPagoId == plan.Id), "existing adjustment audit retained without revision duplication: " + type);
        }
        check((await client.PostAsJsonAsync("/api/comercial/planes/2147483647/cuotas-ajuste", Request(), json)).StatusCode == HttpStatusCode.BadRequest, "missing adjustment plan follows existing HTTP 400 convention");
        foreach (var state in new[] { AcuerdoEstado.Borrador, AcuerdoEstado.Anulado, AcuerdoEstado.Finalizado, AcuerdoEstado.EnCurso })
        {
            await using (var db = create())
            { var v = await db.AcuerdosComercialesVias.SingleAsync(v => v.Id == plan.AcuerdoComercialViaId); v.Estado = state; await db.SaveChangesAsync(); }
            var response = await client.PostAsJsonAsync(route, Request(), json);
            check(response.StatusCode == (state == AcuerdoEstado.EnCurso ? HttpStatusCode.Created : HttpStatusCode.BadRequest), "adjustment uses persisted via state: " + state);
        }
        await using (var db = create())
        { var v = await db.AcuerdosComercialesVias.SingleAsync(v => v.Id == plan.AcuerdoComercialViaId); v.MontoActual += 1; await db.SaveChangesAsync(); }
        var inconsistent = await client.PostAsJsonAsync(route, Request(), json);
        check(inconsistent.StatusCode == HttpStatusCode.BadRequest && (await inconsistent.Content.ReadAsStringAsync()).Contains("total vigente"), "pre-existing amount discrepancy rejects adjustment instead of carrying it forward");
        await using (var db = create())
            check(await db.CuotasComerciales.CountAsync(c => c.PlanPagoId == plan.Id) == 6, "inconsistent adjustment creates no extra quota");

        // A successful ordinary addition must still pass through the new complete revision operation.
        var fresh = await RevisionOperationTests.Seed(create, 2);
        var prep = await RevisionOperationTests.Prepare(create, fresh);
        var revision = RevisionOperationTests.Request(prep, RevisionOperationTests.Add(3)); revision.NuevoMontoActual += 100;
        var revisionResponse = await client.PostAsJsonAsync($"/api/comercial/acuerdos-vias/{fresh.AcuerdoComercialViaId}/plan-pago/revision", revision, json);
        check(revisionResponse.StatusCode == HttpStatusCode.OK, "ordinary addition remains available through authenticated revision endpoint");
        var result = (await revisionResponse.Content.ReadFromJsonAsync<ConfirmacionRevisionPlanResponse>(json))!;
        check(result.Revision.Detalles.Single().TipoCuota == TipoCuota.Cuota && result.EstadoActual.MontoOriginal == 200 && result.EstadoActual.MontoActual == 300 && result.EstadoActual.TotalVigente == 300,
            "ordinary addition has revision audit and consistent unchanged-original balances");
    }
}

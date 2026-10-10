using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetControl.Api.Controllers;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using BudgetControl.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

static class RevisionHttpTests
{
    public static async Task RunAsync(Func<AppDbContext> create, string connectionString, Action<bool, string> check)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("revision-tests-only-signing-key-never-used-by-application-0123456789"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(RevisionesPlanesPagoController).Assembly)
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IRevisionPlanPagoService, RevisionPlanPagoService>();
        builder.Services.AddScoped<IUserContext, CurrentUserService>();
        builder.Services.AddScoped<IComercialService, ComercialService>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
            o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true,
                ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = "revision-tests", ValidAudience = "revision-tests", IssuerSigningKey = key });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
        await app.StartAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            string Token(string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("revision-tests", "revision-tests",
                new[] { new Claim(ClaimTypes.NameIdentifier, "123"), new Claim(ClaimTypes.Name, "admin-jwt"), new Claim(ClaimTypes.Role, role) },
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web); json.Converters.Add(new JsonStringEnumConverter());
            var plan = await RevisionOperationTests.Seed(create, 2);
            var route = $"/api/comercial/acuerdos-vias/{plan.AcuerdoComercialViaId}/plan-pago/revision";
            var query = route + $"?acuerdoId={plan.AcuerdoComercialVia.AcuerdoComercialId}&planId={plan.Id}";
            check((await client.GetAsync(query)).StatusCode == HttpStatusCode.Unauthorized, "HTTP GET unauthenticated = 401");
            check((await client.PostAsJsonAsync(route, new { })).StatusCode == HttpStatusCode.Unauthorized, "HTTP POST unauthenticated = 401");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("User"));
            check((await client.GetAsync(query)).StatusCode == HttpStatusCode.Forbidden, "HTTP GET non-Admin = 403");
            check((await client.PostAsJsonAsync(route, new { })).StatusCode == HttpStatusCode.Forbidden, "HTTP POST non-Admin = 403");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Admin"));
            var response = await client.GetAsync(query);
            check(response.StatusCode == HttpStatusCode.OK, "HTTP Admin JWT prepares revision");
            var prepared = (await response.Content.ReadFromJsonAsync<PreparacionRevisionPlanResponse>(json))!;
            var request = RevisionOperationTests.Request(prepared, RevisionOperationTests.Modify(prepared.Cuotas.Last(), date: prepared.Cuotas.Last().Vencimiento.AddDays(1)));
            // Untrusted extra fields must not become audit values.
            var payload = JsonSerializer.SerializeToNode(request, json)!;
            payload["usuario"] = "forged"; payload["usuarioId"] = "999"; payload["montoAnterior"] = 9999;
            var confirmation = await client.PostAsync(route, new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
            check(confirmation.StatusCode == HttpStatusCode.OK, "HTTP Admin JWT confirms revision");
            var result = (await confirmation.Content.ReadFromJsonAsync<ConfirmacionRevisionPlanResponse>(json))!;
            check(result.Revision.Usuario == "admin-jwt" && result.Revision.UsuarioId == "123" && result.Revision.MontoAnterior == 200, "audit trusts JWT/database, never request user or previous amount");
            request.SolicitudId = Guid.NewGuid();
            var stale = await client.PostAsJsonAsync(route, request, json);
            check(stale.StatusCode == HttpStatusCode.Conflict && (await stale.Content.ReadAsStringAsync()).Contains("error"), "stale HTTP request = 409 with existing error shape");

            var current = await RevisionOperationTests.Prepare(create, plan);
            var lockedRequest = RevisionOperationTests.Request(current, RevisionOperationTests.Modify(current.Cuotas.Last(), date: current.Cuotas.Last().Vencimiento.AddDays(1)));
            await using (var owner = create())
            {
                await using var tx = await owner.Database.BeginTransactionAsync();
                await PlanRevisionLock.AcquireAsync(owner, plan.AcuerdoComercialViaId);
                var timeout = await client.PostAsJsonAsync(route, lockedRequest, json);
                check(timeout.StatusCode == HttpStatusCode.Conflict, "lock timeout maps to controlled HTTP 409");
                await tx.RollbackAsync();
            }
            await using (var verify = create())
                check(!await verify.RevisionesPlanesPago.AnyAsync(r => r.SolicitudId == lockedRequest.SolicitudId), "timed-out confirmation leaves no audit");

            // Hold only the via so a movement can commit while the confirmation waits.
            await using (var owner = create())
            {
                await using var tx = await owner.Database.BeginTransactionAsync();
                await owner.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM acuerdos_comerciales_vias WHERE id={plan.AcuerdoComercialViaId} FOR UPDATE");
                var pending = client.PostAsJsonAsync(route, lockedRequest, json);
                await RevisionOperationTests.AddDependency(create, plan, current.Cuotas.Last().Id, "invoice-active");
                await tx.CommitAsync();
                check((await pending).StatusCode == HttpStatusCode.Conflict, "movement committed while review waits is re-read and rejects withdrawal/edit");
            }
            await AjusteOperationTests.RunAsync(create, client, Token, json, check);
        }
        finally { await app.StopAsync(); }
    }
}

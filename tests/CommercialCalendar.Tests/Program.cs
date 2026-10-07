using System.Reflection;
using System.Text.Json;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

var midnightUtc = new DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
Check(CommercialCalendar.BusinessDate(midnightUtc) == new DateTime(2026, 10, 2), "21:00 Argentina is still the previous business day");
Check(CommercialCalendar.BusinessDate(midnightUtc.AddHours(3)) == new DateTime(2026, 10, 3), "Argentina midnight starts the new day");
var today = CommercialCalendar.Today;
Check(!CommercialCalendar.IsOverdue(today), "deadline today is not overdue");
Check(CommercialCalendar.IsOverdue(today.AddDays(-1)), "deadline yesterday is overdue");
foreach (var serviceType in new[] { typeof(ComercialService), typeof(PagoComercialService) })
{
    var update = serviceType.GetMethod("UpdateCuotaEstado", BindingFlags.NonPublic | BindingFlags.Static)!;
    foreach (var (state, paid, balance, date, expected) in new[] {
        (CuotaEstado.Pendiente, 0m, 100m, today, CuotaEstado.Pendiente),
        (CuotaEstado.Pendiente, 0m, 100m, today.AddDays(-1), CuotaEstado.Vencida),
        (CuotaEstado.Parcial, 20m, 80m, today.AddDays(-1), CuotaEstado.Parcial),
        (CuotaEstado.Pagada, 100m, 0m, today, CuotaEstado.Pagada),
        (CuotaEstado.Anulada, 0m, 0m, today, CuotaEstado.Anulada) })
    {
        var cuota = new CuotaComercial { Estado = state, ImportePagado = paid, SaldoPendiente = balance, FechaVencimiento = date };
        update.Invoke(null, new object[] { cuota });
        Check(cuota.Estado == expected, $"{serviceType.Name}: {state}, paid={paid}, deadline={date:yyyy-MM-dd} => {expected}");
    }
}

if (args.Length != 1)
    throw new ArgumentException("Pass the path to the existing appsettings.json for temporary-table PostgreSQL integration tests.");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = await connection.BeginTransactionAsync();

// Clone structure only, never data. Identity sequences and indexes belong to the
// temporary tables. Restrict search_path so EF cannot write to public tables.
var tables = new[] { "acuerdos_comerciales", "acuerdos_comerciales_vias", "planes_pago", "cuotas_comerciales",
    "pagos_comerciales", "aplicaciones_pago_comerciales", "hitos_comerciales_vias",
    "ajustes_acuerdos_comerciales_vias", "ajustes_cuotas_comerciales",
    "cobranzas", "cobranzas_aplicaciones_facturas", "cobranzas_aplicaciones_obligaciones" };
foreach (var table in tables)
{
    await using var clone = new NpgsqlCommand($"CREATE TEMP TABLE {table} (LIKE public.{table} INCLUDING ALL) ON COMMIT DROP", connection, transaction);
    await clone.ExecuteNonQueryAsync();
}
await using (var restrict = new NpgsqlCommand("SET LOCAL search_path TO pg_temp", connection, transaction))
    await restrict.ExecuteNonQueryAsync();
var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
await using var db = new AppDbContext(options);
await db.Database.UseTransactionAsync(transaction);
var service = new ComercialService(db, new TestUser());

async Task<int> NewVia()
{
    var acuerdo = await service.CreateAcuerdoAsync(new CreateAcuerdoRequest {
        ClienteExternoId = "calendar-test", ObraExternaId = "calendar-test", NumeroAcuerdo = Guid.NewGuid().ToString(),
        FechaAcuerdo = today, Vias = new() { new CreateAcuerdoViaRequest {
            ViaOperacion = ViaOperacion.Via2, ModalidadCobro = ModalidadCobro.Planificada, MontoOriginal = 1000m } }
    });
    return (await db.AcuerdosComercialesVias.SingleAsync(v => v.AcuerdoComercialId == acuerdo.Id)).Id;
}
CreatePlanPagoRequest Request(bool anticipo) => new() {
    TieneAnticipo = anticipo, MontoAnticipo = anticipo ? 200m : 0m,
    FechaAnticipo = anticipo ? today : null, FechaPrimerVencimiento = today.AddDays(15),
    CantidadCuotas = 3, Periodicidad = "Mensual"
};
async Task Reject(Func<Task> action, string name)
{
    try { await action(); }
    catch (InvalidOperationException) { Check(true, name); db.ChangeTracker.Clear(); return; }
    throw new Exception("FAIL: expected rejection: " + name);
}

var without = await service.CrearPlanPagoAsync(await NewVia(), Request(false));
Check(without.Cuotas.Count == 3 && without.Cuotas.All(c => c.TipoCuota == TipoCuota.Cuota), "plan without advance creates only ordinary installments");
var invalidVia = await NewVia();
var missing = Request(true); missing.FechaAnticipo = null;
await Reject(() => service.CrearPlanPagoAsync(invalidVia, missing), "advance without date rejected");
Check(!await db.PlanesPago.AnyAsync(p => p.AcuerdoComercialViaId == invalidVia), "invalid plan not persisted");
var viaId = await NewVia();
var created = await service.CrearPlanPagoAsync(viaId, Request(true));
db.ChangeTracker.Clear();
var persisted = await db.PlanesPago.Include(p => p.Cuotas).SingleAsync(p => p.Id == created.Id);
var advance = persisted.Cuotas.Single(c => c.TipoCuota == TipoCuota.Anticipo);
Check(advance.NumeroCuota == 0 && advance.FechaVencimiento == today && advance.FechaVencimiento.Kind == DateTimeKind.Utc, "EF/PostgreSQL persists advance date and type");
Check(persisted.FechaPrimerVencimiento == today.AddDays(15) && persisted.Cuotas.Single(c => c.NumeroCuota == 1).FechaVencimiento == today.AddDays(15), "EF/PostgreSQL persists independent first installment date");
Check(persisted.Cuotas.Sum(c => c.ImporteOriginal) == 1000m, "rounding preserves plan total");

UpdatePlanPagoRequest Update(bool enabled) => new() {
    TieneAnticipo = enabled, MontoAnticipo = enabled ? 200m : 0m, CantidadCuotas = 3,
    FechaAnticipo = enabled ? today.AddDays(2) : null,
    FechaPrimerVencimiento = today.AddDays(15), Periodicidad = "Mensual",
    Cuotas = persisted.Cuotas.Where(c => enabled || c.TipoCuota != TipoCuota.Anticipo).Select(c => new UpdateCuotaRequest {
        Id = c.Id, FechaVencimiento = c.TipoCuota == TipoCuota.Anticipo ? today.AddDays(2) : c.FechaVencimiento,
        ImporteOriginal = c.TipoCuota == TipoCuota.Anticipo ? 200m : c.NumeroCuota == 3 ? (enabled ? 266.66m : 333.34m) : (enabled ? 266.67m : 333.33m)
    }).ToList()
};
await service.ActualizarPlanPagoAsync(viaId, Update(false));
db.ChangeTracker.Clear();
Check((await db.CuotasComerciales.AsNoTracking().SingleAsync(c => c.Id == advance.Id)).Estado == CuotaEstado.Anulada, "disable annuls existing advance");
var reactivated = await service.ActualizarPlanPagoAsync(viaId, Update(true));
db.ChangeTracker.Clear();
var result = await db.CuotasComerciales.SingleAsync(c => c.Id == advance.Id);
Check(reactivated.Cuotas.Count(c => c.TipoCuota == TipoCuota.Anticipo) == 1 && result.Id == advance.Id, "reactivation reuses the same row");
Check(result.FechaVencimiento == today.AddDays(2) && result.ImporteOriginal == 200m && result.SaldoPendiente == 200m && result.Estado == CuotaEstado.Pendiente, "reactivation uses supplied date, amount, balance and state");
await service.ActualizarPlanPagoAsync(viaId, Update(true));
db.ChangeTracker.Clear();
Check((await db.CuotasComerciales.SingleAsync(c => c.Id == advance.Id)).FechaVencimiento == today.AddDays(2), "save and reload preserves date");
result = await db.CuotasComerciales.SingleAsync(c => c.Id == advance.Id);
result.ImportePagado = 100m; result.SaldoPendiente = 100m; result.Estado = CuotaEstado.Parcial;
await db.SaveChangesAsync();
await Reject(() => service.ActualizarPlanPagoAsync(viaId, Update(false)), "cannot remove partially paid advance");
var tooSmall = Update(true); tooSmall.MontoAnticipo = 50m;
tooSmall.Cuotas.RemoveAll(c => c.Id == advance.Id); // Exercise the authoritative header and paid-amount guard.
await Reject(() => service.ActualizarPlanPagoAsync(viaId, tooSmall), "cannot reduce advance below paid amount");
result = await db.CuotasComerciales.SingleAsync(c => c.Id == advance.Id);
result.ImportePagado = 200m; result.SaldoPendiente = 0m; result.Estado = CuotaEstado.Pagada;
await db.SaveChangesAsync();
await Reject(() => service.ActualizarPlanPagoAsync(viaId, Update(true)), "cannot edit a paid advance");
var noAdvanceRequest = Request(false); noAdvanceRequest.FechaAnticipo = today;
var ignored = await service.CrearPlanPagoAsync(await NewVia(), noAdvanceRequest);
Check(ignored.Cuotas.All(c => c.TipoCuota != TipoCuota.Anticipo), "date alone does not create advance when flag is false");
var queryCuota = await db.CuotasComerciales.SingleAsync(c => c.Id == ignored.Cuotas[0].Id);
queryCuota.FechaVencimiento = today;
await db.SaveChangesAsync();
Check(!(await service.GetCuotasVencidasAsync()).Any(c => c.Id == queryCuota.Id), "PostgreSQL overdue query excludes today");
queryCuota.FechaVencimiento = today.AddDays(-1);
await db.SaveChangesAsync();
Check((await service.GetCuotasVencidasAsync()).Any(c => c.Id == queryCuota.Id && c.Estado == CuotaEstado.Vencida), "PostgreSQL overdue query includes yesterday");
// Activate a plan that never had an advance. Header fields are the sole source.
var activationVia = await NewVia();
var basePlan = await service.CrearPlanPagoAsync(activationVia, Request(false));
UpdatePlanPagoRequest Activation(DateTime? date) => new() {
    TieneAnticipo = true, MontoAnticipo = 200m, FechaAnticipo = date,
    CantidadCuotas = 3, FechaPrimerVencimiento = today.AddDays(15), Periodicidad = "Mensual",
    Cuotas = basePlan.Cuotas.Select(c => new UpdateCuotaRequest {
        Id = c.Id, FechaVencimiento = c.FechaVencimiento,
        ImporteOriginal = c.NumeroCuota == 3 ? 266.66m : 266.67m
    }).ToList()
};
foreach (var invalidDate in new DateTime?[] { null, DateTime.MinValue, DateTime.MinValue.AddHours(12) })
{
    try {
        await service.ActualizarPlanPagoAsync(activationVia, Activation(invalidDate));
        throw new Exception("Missing invalid advance date rejection");
    } catch (InvalidOperationException ex) {
        Check(ex.Message.Contains("fecha de vencimiento del anticipo"), "activation rejects absent/minimum date with functional message");
        db.ChangeTracker.Clear();
    }
}
Check(!await db.CuotasComerciales.AnyAsync(c => c.PlanPagoId == basePlan.Id && c.TipoCuota == TipoCuota.Anticipo), "invalid activation creates no advance row");
var activationDate = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);
var activated = await service.ActualizarPlanPagoAsync(activationVia, Activation(activationDate));
var activationId = activated.Cuotas.Single(c => c.TipoCuota == TipoCuota.Anticipo).Id;
db.ChangeTracker.Clear();
var activatedRow = await db.CuotasComerciales.AsNoTracking().SingleAsync(c => c.Id == activationId);
Check(activatedRow.NumeroCuota == 0 && activatedRow.FechaVencimiento == activationDate, "first activation persists explicit advance date and number zero");
Check(activated.FechaPrimerVencimiento == today.AddDays(15) && activated.Cuotas.Single(c => c.NumeroCuota == 1).FechaVencimiento == today.AddDays(15), "activation leaves first installment date independent");
await service.ActualizarPlanPagoAsync(activationVia, Activation(activationDate));
db.ChangeTracker.Clear();
Check(await db.CuotasComerciales.CountAsync(c => c.PlanPagoId == basePlan.Id && c.TipoCuota == TipoCuota.Anticipo) == 1, "repeated activation does not duplicate advance");
var conflict = Activation(activationDate);
conflict.Cuotas.Add(new UpdateCuotaRequest { Id = activationId, FechaVencimiento = activationDate.AddDays(1), ImporteOriginal = 200m });
try {
    await service.ActualizarPlanPagoAsync(activationVia, conflict);
    throw new Exception("Missing contradictory date rejection");
} catch (InvalidOperationException ex) {
    Check(ex.Message.Contains("deben coincidir"), "conflicting duplicate advance date rejected");
    db.ChangeTracker.Clear();
}
var disabled = Activation(null); disabled.TieneAnticipo = false; disabled.MontoAnticipo = 0;
foreach (var cuota in disabled.Cuotas) cuota.ImporteOriginal = basePlan.Cuotas.Single(c => c.Id == cuota.Id).ImporteOriginal;
await service.ActualizarPlanPagoAsync(activationVia, disabled);
db.ChangeTracker.Clear();
var disabledRow = await db.CuotasComerciales.AsNoTracking().SingleAsync(c => c.Id == activationId);
Check(disabledRow.Estado == CuotaEstado.Anulada && disabledRow.FechaVencimiento == activationDate, "disable requires no date and preserves stored deadline");
var reactivationDate = today.AddDays(-1);
await service.ActualizarPlanPagoAsync(activationVia, Activation(reactivationDate));
db.ChangeTracker.Clear();
var reused = await db.CuotasComerciales.SingleAsync(c => c.Id == activationId);
Check(reused.Estado == CuotaEstado.Vencida && reused.FechaVencimiento == reactivationDate && reused.SaldoPendiente == 200m, "reactivation reuses ID and applies explicit overdue date");
Check(await db.CuotasComerciales.CountAsync(c => c.PlanPagoId == basePlan.Id && c.TipoCuota == TipoCuota.Anticipo) == 1, "reactivation still has exactly one advance");
reused.ImportePagado = 100m; reused.SaldoPendiente = 100m; reused.Estado = CuotaEstado.Parcial;
await db.SaveChangesAsync();
await service.ActualizarPlanPagoAsync(activationVia, Activation(today.AddDays(2)));
db.ChangeTracker.Clear();
reused = await db.CuotasComerciales.SingleAsync(c => c.Id == activationId);
Check(reused.Estado == CuotaEstado.Parcial && reused.ImportePagado == 100m && reused.SaldoPendiente == 100m, "explicit date update preserves partial payment and state");
reused.ImportePagado = 200m; reused.SaldoPendiente = 0m; reused.Estado = CuotaEstado.Pagada;
await db.SaveChangesAsync();
await Reject(() => service.ActualizarPlanPagoAsync(activationVia, Activation(today.AddDays(3))), "paid advance cannot change date through header");
await service.ActualizarPlanPagoAsync(activationVia, Activation(today.AddDays(2)));
db.ChangeTracker.Clear();
reused = await db.CuotasComerciales.SingleAsync(c => c.Id == activationId);
Check(reused.Estado == CuotaEstado.Pagada && reused.SaldoPendiente == 0m && reused.FechaVencimiento == today.AddDays(2), "unchanged paid advance remains intact when editing plan");
await CommercialReportTests.RunAsync(db, connection, transaction, Check);
await transaction.RollbackAsync();
Console.WriteLine($"{checks} checks passed; temporary PostgreSQL tables rolled back.");

sealed class TestUser : IUserContext
{
    public string UserName => "calendar-regression";
    public string? UserId => null;
}

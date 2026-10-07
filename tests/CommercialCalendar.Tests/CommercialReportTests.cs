using BudgetControl.Api.Controllers;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

static class CommercialReportTests
{
    public static async Task RunAsync(AppDbContext seed, NpgsqlConnection connection, NpgsqlTransaction transaction, Action<bool, string> check)
    {
        // All writes are fixture setup in the caller's isolated temporary tables.
        foreach (var agreement in await seed.AcuerdosComerciales.ToListAsync())
            agreement.Estado = AcuerdoEstado.Finalizado;
        await seed.SaveChangesAsync();
        var today = CommercialCalendar.Today;
        var start = today.AddDays(-2);
        var end = today;
        AcuerdoComercialVia NewVia(string client, string currency, ViaOperacion type, decimal amount = 1000m)
        {
            var agreement = new AcuerdoComercial {
                ClienteExternoId = client, ObraExternaId = "report-test", NumeroAcuerdo = Guid.NewGuid().ToString(),
                FechaAcuerdo = today, FechaAlta = today, UsuarioAlta = "report-test", Estado = AcuerdoEstado.EnCurso,
                MontoTotal = amount, ViaOperacion = type
            };
            var via = new AcuerdoComercialVia {
                AcuerdoComercial = agreement, MonedaCodigo = currency, ViaOperacion = type,
                MontoActual = amount, MontoOriginal = amount, Estado = AcuerdoEstado.EnCurso,
                FechaAlta = today, UsuarioAlta = "report-test"
            };
            via.PlanPago = new PlanPago { AcuerdoComercialVia = via, Periodicidad = "Mensual", FechaPrimerVencimiento = today };
            seed.AcuerdosComercialesVias.Add(via);
            return via;
        }
        CuotaComercial Obligation(AcuerdoComercialVia via, decimal balance, DateTime date, CuotaEstado state = CuotaEstado.Pendiente, TipoCuota type = TipoCuota.Cuota)
        {
            var obligation = new CuotaComercial {
                PlanPago = via.PlanPago!, NumeroCuota = type == TipoCuota.Anticipo ? 0 : via.PlanPago!.Cuotas.Count + 1,
                TipoCuota = type, FechaVencimiento = date, SaldoPendiente = balance,
                ImporteOriginal = balance + (state == CuotaEstado.Parcial ? 20m : 0m),
                ImportePagado = state == CuotaEstado.Parcial ? 20m : 0m, Estado = state
            };
            via.PlanPago!.Cuotas.Add(obligation);
            return obligation;
        }
        void Payment(AcuerdoComercialVia via, decimal amount, DateTime date, bool cancelled = false)
        {
            seed.PagosComerciales.Add(new PagoComercial {
                AcuerdoComercialVia = via, AcuerdoComercial = via.AcuerdoComercial,
                ClienteExternoId = via.AcuerdoComercial.ClienteExternoId, ObraExternaId = "report-test",
                MonedaCodigo = via.MonedaCodigo, ImporteTotal = amount, FechaPago = date,
                MedioPago = "Efectivo", FechaAlta = today, UsuarioAlta = "report-test",
                Estado = cancelled ? PagoEstado.Anulado : PagoEstado.Registrado
            });
        }
        void Collection(CuotaComercial obligation, decimal amount, DateTime date, CobranzaEstado state = CobranzaEstado.Confirmada)
        {
            seed.CobranzasAplicacionesObligacion.Add(new CobranzaAplicacionObligacion {
                CuotaComercial = obligation, TipoObligacion = "Cuota", ImporteAplicado = amount,
                FechaAlta = today, UsuarioAlta = "report-test",
                AplicacionFactura = new CobranzaAplicacionFactura {
                    VentaId = 1, ImporteAplicado = amount, FechaAlta = today, UsuarioAlta = "report-test",
                    Cobranza = new Cobranza {
                        ClienteExternoId = obligation.PlanPago.AcuerdoComercialVia.AcuerdoComercial.ClienteExternoId,
                        MonedaCodigo = obligation.PlanPago.AcuerdoComercialVia.MonedaCodigo,
                        Fecha = date, ImporteTotal = amount, Estado = state, FechaAlta = today, UsuarioAlta = "report-test"
                    }
                }
            });
        }

        var ars = NewVia("report-ars", "ARS", ViaOperacion.Via2);
        ars.PlanPago!.TieneAnticipo = true; ars.PlanPago.MontoAnticipo = 80m;
        Obligation(ars, 80m, start, CuotaEstado.Parcial, TipoCuota.Anticipo);
        Obligation(ars, 120m, end); // both inclusive boundaries
        Obligation(ars, 200m, start.AddDays(-1));
        var stale = Obligation(ars, 300m, end.AddDays(1), CuotaEstado.Vencida);
        Obligation(ars, 400m, end, CuotaEstado.Anulada);
        Obligation(ars, 0m, end, CuotaEstado.Pagada);
        var partialFuture = Obligation(ars, 40m, end.AddDays(2), CuotaEstado.Parcial);
        Payment(ars, 10m, start.AddDays(-1)); Payment(ars, 20m, start);
        Payment(ars, 30m, end.AddTicks(TimeSpan.TicksPerDay - 10)); Payment(ars, 40m, end.AddDays(1));
        Payment(ars, 999m, end, true);
        var usd = NewVia("report-usd", "USD", ViaOperacion.Via1);
        var usdObligation = Obligation(usd, 250m, start);
        Collection(usdObligation, 10m, start.AddDays(-1)); Collection(usdObligation, 20m, start);
        Collection(usdObligation, 30m, end.AddTicks(TimeSpan.TicksPerDay - 10)); Collection(usdObligation, 40m, end.AddDays(1));
        Collection(usdObligation, 999m, end, CobranzaEstado.Anulada);
        Collection(usdObligation, 999m, end, CobranzaEstado.Borrador);
        Payment(usd, 999m, end); // mirrored commercial payments must not double-count Via1

        foreach (var type in new[] { ViaOperacion.Via1, ViaOperacion.Via2 })
        foreach (var state in new[] { AcuerdoEstado.Finalizado, AcuerdoEstado.Anulado })
        foreach (var closeAgreement in new[] { true, false })
        {
            var excluded = NewVia("excluded", "ARS", type);
            if (closeAgreement) excluded.AcuerdoComercial.Estado = state; else excluded.Estado = state;
            var obligation = Obligation(excluded, 999m, start);
            Payment(excluded, 999m, end); Collection(obligation, 999m, end);
        }
        await seed.SaveChangesAsync();
        seed.ChangeTracker.Clear();
        var before = await seed.CuotasComerciales.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Estado, c.ImportePagado, c.SaldoPendiente, c.FechaVencimiento }).ToListAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
        await using var readDb = new NoReportWritesContext(options);
        await readDb.Database.UseTransactionAsync(transaction);
        var service = new ComercialService(readDb, new TestUser());
        var controller = new ReportesComercialesController(service);
        check(await controller.GetResumen(null, end, null) is BadRequestObjectResult, "report rejects only hasta with HTTP 400");
        check(await controller.GetResumen(start, null, null) is BadRequestObjectResult, "report rejects only desde with HTTP 400");
        check(await controller.GetResumen(end, start, null) is BadRequestObjectResult, "report rejects inverted range with HTTP 400");
        check(await controller.GetResumen(null, null, "99") is BadRequestObjectResult, "report rejects undefined via");
        foreach (var (from, to) in new (DateTime?, DateTime?)[] { (null, end), (start, null), (end, start) })
        {
            var rejected = false;
            try { await service.GetReporteComercialResumenAsync(from, to); }
            catch (InvalidOperationException) { rejected = true; }
            check(rejected, "service validates range independently of controller");
        }
        var result = await controller.GetResumen(null, null, "Todos") as OkObjectResult;
        var general = (ReporteComercialResumenResponse)result!.Value!;
        var period = await service.GetReporteComercialResumenAsync(start, end);
        var singleDay = await service.GetReporteComercialResumenAsync(end, end);
        check(singleDay.TotalesPorMoneda.All(t => t.TotalCobradoPeriodo == 30m), "same-day period includes the final PostgreSQL microsecond, excludes next midnight");
        check(general.Alcance == "General" && general.PeriodoDesde == null && general.PeriodoHasta == null, "general has no implicit date range");
        check(period.Alcance == "Periodo" && period.PeriodoDesde == start && period.PeriodoHasta == end, "period returns applied calendar bounds");
        check(general.AcuerdosActivos == 2 && general.TotalesPorMoneda.Count == 2, "same active universe excludes closed agreements and vias in both payment sources");
        var ga = general.TotalesPorMoneda.Single(t => t.MonedaCodigo == "ARS");
        var gu = general.TotalesPorMoneda.Single(t => t.MonedaCodigo == "USD");
        var pa = period.TotalesPorMoneda.Single(t => t.MonedaCodigo == "ARS");
        var pu = period.TotalesPorMoneda.Single(t => t.MonedaCodigo == "USD");
        check(ga.TotalCobradoPeriodo == 100m && gu.TotalCobradoPeriodo == 100m && pa.TotalCobradoPeriodo == 50m && pu.TotalCobradoPeriodo == 50m, "accumulated vs inclusive period payments, no cancelled or mirrored payments");
        check(ga.TotalPorCobrarPeriodo == 740m && pa.TotalPorCobrarPeriodo == 200m && gu.TotalPorCobrarPeriodo == 250m, "general includes all obligations; period includes both deadlines and partial advance, excludes cancelled/paid");
        check(ga.TotalAcordadoActivo == 1000m && ga.SaldoTotalClientes == 900m && pa.SaldoTotalClientes == 900m && pa.TotalAcordadoActivo == 1000m, "agreement debt remains current accumulated in period mode");
        check(ga.TotalVencido == 280m && pa.TotalVencido == 280m && gu.TotalVencido == 250m, "overdue uses current Argentina calendar independent of period");
        check(ga.CuotasPendientesPeriodo == 5 && pa.CuotasPendientesPeriodo == 2 && gu.CuotasPendientesPeriodo == 1 && ga.CuotasVencidas == 2 && gu.CuotasVencidas == 1, "counts belong to each currency");
        check(general.TotalAcordadoActivo == 0 && general.TotalCobradoPeriodo == 0 && general.SaldoTotalClientes == 0 && general.TotalPorCobrarPeriodo == 0 && general.TotalVencido == 0, "compatibility amounts never sum currencies");
        check(general.ProximosVencimientos.Single(c => c.CuotaId == stale.Id).Estado == CuotaEstado.Pendiente && general.ProximosVencimientos.Single(c => c.CuotaId == partialFuture.Id).Estado == CuotaEstado.Parcial, "derived current display state preserves partial without modifying stored state");
        check(period.ProximosVencimientos.Select(c => c.CuotaId).SequenceEqual(general.ProximosVencimientos.Select(c => c.CuotaId)), "next deadlines are current, not restricted to period");
        foreach (var type in new[] { ViaOperacion.Via1, ViaOperacion.Via2 })
        {
            var filtered = await service.GetReporteComercialResumenAsync(null, null, type);
            check(filtered.AcuerdosActivos == 1 && filtered.TotalCobradoPeriodo == 100m && filtered.TotalesPorMoneda.Count == 1, "via filter uses one active universe and single-currency compatibility");
        }
        check(!readDb.ChangeTracker.Entries().Any(), "GET loads no tracked entities and never calls SaveChanges");
        var after = await seed.CuotasComerciales.AsNoTracking().OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Estado, c.ImportePagado, c.SaldoPendiente, c.FechaVencimiento }).ToListAsync();
        check(before.SequenceEqual(after), "GET preserves persisted dates, balances and states");

        var netting = NewVia("netting", "ARS", ViaOperacion.Via2, 500m);
        NewVia("netting", "ARS", ViaOperacion.Via2, 500m);
        Payment(netting, 600m, today);
        var prepaid = NewVia("prepaid", "USD", ViaOperacion.Via2, 100m);
        Payment(prepaid, 150m, today);
        // A payment in another currency must not reduce an ARS agreement balance.
        var otherCurrency = NewVia("currency-isolation", "ARS", ViaOperacion.Via2, 100m);
        Payment(otherCurrency, 50m, today);
        seed.ChangeTracker.Entries<PagoComercial>().Single(e => e.Entity.AcuerdoComercialVia == otherCurrency).Entity.MonedaCodigo = "USD";
        await seed.SaveChangesAsync();
        var grouped = await service.GetReporteComercialResumenAsync(null, null);
        check(grouped.ClientesConDeuda.Single(c => c.ClienteExternoId == "netting").SaldoPendiente == 400m, "debt floor is applied per customer/currency, not per via");
        check(grouped.ClientesConDeuda.All(c => c.ClienteExternoId != "prepaid"), "overpaid customer contributes no negative debt");
        check(grouped.ClientesConDeuda.Single(c => c.ClienteExternoId == "currency-isolation").SaldoPendiente == 100m, "payments in a different currency do not reduce agreement debt");

        foreach (var currency in new[] { "ARS", "USD" })
            for (var i = 0; i < 11; i++) NewVia($"ranking-{currency}-{i}", currency, ViaOperacion.Via2, 2000m + i);
        await seed.SaveChangesAsync();
        var ranked = await service.GetReporteComercialResumenAsync(null, null);
        check(ranked.ClientesConDeuda.Count == 20 && ranked.ClientesConDeuda.GroupBy(c => c.MonedaCodigo).All(g => g.Count() == 10 && g.First().SaldoPendiente == 2010m && g.Last().SaldoPendiente == 2001m), "ranking returns ten largest debtors per currency, not ten globally");
    }

    private sealed class NoReportWritesContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new Exception("Report attempted SaveChanges");
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => throw new Exception("Report attempted SaveChangesAsync");
    }
}

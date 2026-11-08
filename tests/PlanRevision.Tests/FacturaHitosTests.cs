using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Commercial;
using BudgetControl.Api.Services.Sales;
using BudgetControl.Api.Services.Collections;
using BudgetControl.Api.Services.Accounting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

static class FacturaHitosTests
{
    public static async Task RunAsync(Func<AppDbContext> create, Action<bool, string> check)
    {
        var now = DateTime.UtcNow;
        CurrentUserService User() => new(RevisionOperationTests.Identity());
        var plan = await RevisionOperationTests.Seed(create, 2);
        int a, b;
        await using (var db = create())
        {
            var via = await db.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == plan.AcuerdoComercialViaId);
            via.ViaOperacion = ViaOperacion.Via1; via.ModalidadCobro = ModalidadCobro.Abierta;
            via.AcuerdoComercial.ClienteExternoId = "1234"; via.AcuerdoComercial.ObraExternaId = "9876";
            db.PlanesPago.Remove(await db.PlanesPago.SingleAsync(p => p.Id == plan.Id));
            var first = new HitoComercialVia { Descripcion = "Primero", ImporteEstimado = 60, FechaReferencia = now.AddDays(-1), FechaAlta = now, UsuarioAlta = "test" };
            var second = new HitoComercialVia { Descripcion = "Segundo", ImporteEstimado = 140, FechaReferencia = now, FechaAlta = now, UsuarioAlta = "test" };
            via.Hitos.Add(first); via.Hitos.Add(second); await db.SaveChangesAsync(); a = first.Id; b = second.Id;
        }
        await using var lookup = create();
        var punto = await lookup.PuntosVentaComprobantes.Include(p => p.PuntoVenta).SingleAsync(p => p.PuntoVenta.Numero == 321);
        var iva = await lookup.AlicuotasIvaVenta.SingleAsync(t => t.Codigo == "T0");
        FacturaSimpleRequest Request(int number, decimal net = 100) => new() { ClienteExternoId = "1234", ObraExternaId = "9876", FechaComprobante = now,
            PuntoVenta = 321, NumeroComprobante = number, PuntoVentaComprobanteId = punto.Id, TipoComprobanteVentaId = punto.TipoComprobanteVentaId,
            Lineas = new() { new(iva.Id, net) }, AplicacionesHitos = new() { new() { HitoId = a, Importe = 60 }, new() { HitoId = b, Importe = 40 } } };
        async Task Reject(FacturaSimpleRequest r, string name)
        {
            await using var db = create();
            try { await FacturaSimpleTests.CreateService(db).RegistrarSimpleAsync(r); throw new Exception("Accepted invalid hitos invoice: " + name); }
            catch (InvalidOperationException) { check(true, name); }
            check(!await db.Ventas.AnyAsync(v => v.NumeroComprobante == r.NumeroComprobante && v.PuntoVentaComprobanteId == punto.Id), "rejected hitos invoice rolls back completely: " + name);
        }
        var missing = Request(8800); missing.AplicacionesHitos.Clear(); await Reject(missing, "open invoice requires actual hito allocation");
        var duplicate = Request(8800); duplicate.AplicacionesHitos[1].HitoId = a; await Reject(duplicate, "duplicate hito allocation rejected");
        var incomplete = Request(8800); incomplete.AplicacionesHitos[1].Importe = 39; await Reject(incomplete, "hitos must allocate exact invoice net");
        var excessive = Request(8800); excessive.AplicacionesHitos[0].Importe = 61; excessive.AplicacionesHitos[1].Importe = 39; await Reject(excessive, "hito amount cap enforced");
        var mixed = Request(8800); mixed.Aplicaciones.Add(new() { ObligacionId = 1, ImporteAplicado = 100 }); await Reject(mixed, "one allocation modality per invoice");
        VentaResponse venta;
        await using (var db = create()) venta = await FacturaSimpleTests.CreateService(db).RegistrarSimpleAsync(Request(8800));
        check(venta.Estado == VentaEstado.Confirmada && venta.Total == 121, "invoice confirms with net allocated to hitos and VAT kept on invoice");
        async Task<Dictionary<int, SaldoHitoVia1>> Balances()
        { await using var db = create(); return await SaldosHitosVia1.ObtenerAsync(db, new[] { a, b }); }
        var balances = await Balances();
        check(balances[a].Facturado == 60 && balances[b].Facturado == 40 && balances[a].Cobrado == 0, "confirmed invoice appears as facturado, not cobrado");
        await using (var db = create())
        {
            try { await new ComercialService(db, User()).EliminarHitoAsync(plan.AcuerdoComercialViaId, a); throw new Exception("Deleted invoiced hito"); }
            catch (InvalidOperationException) { check(true, "invoiced hito deletion blocked"); }
        }
        await using (var db = create())
        {
            try { await new ComercialService(db, User()).ActualizarHitoAsync(plan.AcuerdoComercialViaId, a, new() { Descripcion = "Reduced", ImporteEstimado = 59, FechaReferencia = now }); throw new Exception("Reduced invoiced hito"); }
            catch (InvalidOperationException) { check(true, "hito cannot be reduced below invoiced amount"); }
        }
        var over = Request(8801, 101); over.AplicacionesHitos = new() { new() { HitoId = b, Importe = 101 } }; await Reject(over, "already invoiced net reduces hito availability");
        // Draft reservations are excluded only for their own invoice.
        int draftId;
        await using (var db = create())
        {
            var service = FacturaSimpleTests.CreateService(db);
            draftId = (await service.CreateVentaAsync(Request(8802, 80))).Id;
            var draft = await db.Ventas.FindAsync(draftId);
            draft!.FiscalSimpleJson = JsonSerializer.Serialize(new FiscalRegistrado(new() { new(iva.Id, 80, 21, 16.8m) }, 0, 0, 0, ImputaNetoPlan: true));
            new CalculadorVentasService().RecalcularTotales(draft); await db.SaveChangesAsync();
            await service.UpdateVinculacionesHitosAsync(draftId, new() { new() { HitoId = b, Importe = 80 } });
            await service.UpdateVinculacionesHitosAsync(draftId, new() { new() { HitoId = b, Importe = 80 } });
        }
        check((await Balances())[b].Reservado == 80, "draft reserves net once and can be edited without double counting itself");
        var reserved = Request(8801, 21); reserved.AplicacionesHitos = new() { new() { HitoId = b, Importe = 21 } }; await Reject(reserved, "draft reservation prevents duplicate invoicing");
        await using (var db = create()) await FacturaSimpleTests.CreateService(db).UpdateVinculacionesHitosAsync(draftId, new());
        check((await Balances())[b].Reservado == 0, "removing draft allocation releases reservation");
        async Task<int> Cobrar(decimal amount, bool cheque = false)
        {
            await using var db = create(); var service = FacturaSimpleTests.CreateCobrosService(db);
            var medio = await db.MediosPagoCobranza.SingleAsync(m => m.Codigo == (cheque ? "CHEQUE" : "EFECTIVO"));
            var banco = await db.BancosCobranza.Select(x => x.Id).FirstAsync();
            var cob = await service.CreateCobranzaAsync(new() { ClienteExternoId = "1234", Fecha = now, ImporteTotal = amount });
            await service.AddMedioPagoAsync(cob.Id, new() { MedioPagoCobranzaId = medio.Id, Importe = amount, BancoCobranzaId = cheque ? banco : null, FechaEmision = now, FechaValor = now });
            await service.AddAplicacionFacturaAsync(cob.Id, new() { VentaId = venta.Id, ImporteAplicado = amount });
            await service.ConfirmarCobranzaAsync(cob.Id); return cob.Id;
        }
        await Cobrar(60.5m);
        balances = await Balances(); check(balances[a].Cobrado == 50 && balances[b].Cobrado == 0, "partial gross invoice collection allocates net in stable hito order");
        var chequeId = await Cobrar(60.5m, true);
        balances = await Balances(); check(balances[a].Cobrado == 60 && balances[b].Cobrado == 40, "full gross collection settles exactly allocated hito net");
        await using (var db = create())
        {
            var cheque = await db.ChequesTerceros.Include(x => x.CobranzaMedioPago).SingleAsync(x => x.CobranzaMedioPago.CobranzaId == chequeId);
            cheque.Estado = ChequeTerceroEstado.DEPOSITADO; await db.SaveChangesAsync();
            var user = User();
            var config = new ConfiguracionesContablesService(db, user);
            var auto = new ContabilizacionAutomaticaService(db, config, new AsientosContablesService(db, user));
            await new CarteraChequesService(db, user, auto).RechazarAsync(cheque.Id, new() { FechaRechazo = now, Motivo = "Prueba hitos" });
        }
        balances = await Balances(); check(balances[a].Cobrado == 50 && balances[b].Cobrado == 0, "rejected cheque restores hito outstanding amount");
        var replacement = await Cobrar(60.5m);
        await using (var db = create()) await FacturaSimpleTests.CreateCobrosService(db).AnularCobranzaAsync(replacement, new() { Motivo = "Prueba" });
        balances = await Balances(); check(balances[a].Cobrado == 50 && balances[b].Cobrado == 0, "cancelled replacement collection excluded from hito paid total");
        await using (var db = create())
        {
            var comercial = new ComercialService(db, User());
            var detalle = await comercial.GetAcuerdoDetalleAsync(plan.AcuerdoComercialVia.AcuerdoComercialId);
            var via = detalle!.Vias.Single(v => v.Id == plan.AcuerdoComercialViaId);
            check(via.TotalPagado == 50 && via.SaldoPendiente == 150 && via.Hitos.Single(h => h.Id == a).ImporteFacturado == 60 && via.Hitos.Single(h => h.Id == a).ImporteAplicado == 50,
                "agreement shows invoice and collection totals from its hitos");
            check((await comercial.GetEstadoComercialViaAsync(via.Id)).TotalPagado == 50, "agreement summary agrees with hito collection totals");
            var trazabilidad = await DetalleHitoVia1.ObtenerAsync(db, detalle.Id, via.Id, a);
            var factura = trazabilidad.Single(f => f.VentaId == venta.Id);
            check(factura.NetoHito == 60 && factura.CobradoHito == 50 && factura.TotalFactura == 121,
                "hito trace separates invoice gross, allocated net and collected net");
            check(factura.Cobranzas.Sum(c => c.NetoHito) == via.Hitos.Single(h => h.Id == a).ImporteAplicado,
                "hito trace reconciles collection rows with agreement summary");
            check(factura.Cobranzas.Single(c => c.CobranzaId == chequeId).NetoHito == 0 && factura.Cobranzas.Single(c => c.CobranzaId == chequeId).Rechazado == 60.5m &&
                factura.Cobranzas.Single(c => c.CobranzaId == replacement).NetoHito == 0,
                "trace shows rejected and cancelled collections without counting them as paid");
            var segundo = await DetalleHitoVia1.ObtenerAsync(db, detalle.Id, via.Id, b);
            check(segundo.Single(f => f.VentaId == venta.Id).CobradoHito == 0, "trace respects allocation order across all invoice hitos");
            try { await DetalleHitoVia1.ObtenerAsync(db, -1, via.Id, a); throw new Exception("Cross-agreement hito exposed"); }
            catch (InvalidOperationException) { check(true, "hito trace enforces agreement ownership"); }
        }
    }
}

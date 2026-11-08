using BudgetControl.Api.Services.Collections;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Accounting;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Accounting;
using BudgetControl.Api.Services.Sales;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

static class FacturaSimpleTests
{
    private sealed class Clientes : IExternalDataService
    {
        public Task<Client?> GetClientByIdAsync(int id) => Task.FromResult<Client?>(new Client { IdCliente=id });
        public Task<Obra?> GetObraByIdAsync(int id) => Task.FromResult<Obra?>(new Obra { IdObra=id, ClienteId=1234 });
        public Task<IEnumerable<Client>> GetClientsAsync() => Task.FromResult<IEnumerable<Client>>(new[] { new Client { IdCliente = 1234 } });
        public Task<IEnumerable<Obra>> GetObrasAsync() => Task.FromResult<IEnumerable<Obra>>(new[] { new Obra { IdObra = 56, ClienteId = 1234 } });
        public Task<IEnumerable<Obra>> GetObrasByClientAsync(int id) => throw new NotSupportedException();
    }
    internal static VentasService CreateService(AppDbContext ctx)
    {
        var user = new CurrentUserService(RevisionOperationTests.Identity());
        var config = new ConfiguracionesContablesService(ctx,user);
        var ext = new Clientes(); var calc = new CalculadorVentasService();
        return new(ctx,ext,user,calc,new ContabilizacionAutomaticaService(ctx,config,new AsientosContablesService(ctx,user)),config,new PercepcionIibbService(ctx,ext,user,calc));
    }
    internal static CobranzasService CreateCobrosService(AppDbContext ctx)
    {
        var user = new CurrentUserService(RevisionOperationTests.Identity());
        var config = new ConfiguracionesContablesService(ctx,user); var asientos = new AsientosContablesService(ctx,user);
        var auto = new ContabilizacionAutomaticaService(ctx,config,asientos);
        return new(ctx,user,new Clientes(),auto,asientos,config,new CarteraChequesService(ctx,user,auto));
    }
    internal static async Task Run(Func<AppDbContext> create, Action<bool,string> check)
    {
        var now = DateTime.UtcNow;
        await using var db = create();
        var punto = new PuntoVentaComprobante { UsuarioAlta="test", FechaAlta=now, PuntoVenta=new(){Numero=321, Descripcion="Test", UsuarioAlta="test",FechaAlta=now}, TipoComprobante=new(){Codigo="FST",Descripcion="Factura", FechaAlta=now} };
        var tasas = new[]{21m,10.5m}.Select((n,i)=>new AlicuotaIvaVenta {Codigo="T"+i, Descripcion="Test", Porcentaje=n, UsuarioAlta="test",FechaAlta=now}).ToArray();
        db.Add(punto); db.AddRange(tasas);
        db.Add(new ClientePercepcionIibbConfig { ClienteExternoId="1234", Situacion=SituacionPercepcionIibbCliente.Alcanzado, UsuarioAlta="test", FechaAlta=now,
            RegimenPercepcionIibb=new(){Codigo="T",Descripcion="Test", NumeroRegimen="1", Porcentaje=2,VigenciaDesde=now.Date.AddDays(-1),UsuarioAlta="test",FechaAlta=now} });
        await db.SaveChangesAsync();
        var r = new FacturaSimpleRequest {ClienteExternoId="1234",ObraExternaId="56",FechaComprobante=now,NumeroComprobante=1,PuntoVenta=321,PuntoVentaComprobanteId=punto.Id,TipoComprobanteVentaId=punto.TipoComprobanteVentaId, Lineas=new(){new(tasas[0].Id,100),new(tasas[1].Id,200)},NoGravado=10,Exento=5,DevolucionCheques=20};
        VentasService Service(AppDbContext ctx) => CreateService(ctx);
        await using (var failed = create()) {
            try { await Service(failed).RegistrarSimpleAsync(r); throw new Exception("Missing accounting configuration accepted"); }
            catch(InvalidOperationException ex) when(ex.Message.Contains("FACTURA_VENTA")) { }
        }
        check(!await db.Ventas.AnyAsync(v=>v.ClienteExternoId=="1234"),"accounting validation failure rolls back invoice");
        var cfg=new ConfiguracionContable {CodigoOperacion="FACTURA_VENTA",Descripcion="Test",UsuarioAlta="test",FechaAlta=now};
        foreach(var concepto in new[]{"CLIENTES","VENTA_NETA","IVA_DEBITO","PERCEPCION_IIBB"}) cfg.Detalles.Add(new(){Concepto=concepto,TipoMovimiento=concepto=="CLIENTES"?"Debe":"Haber",Orden=cfg.Detalles.Count+1,CuentaContable=new(){Codigo="T"+cfg.Detalles.Count,Nombre=concepto,TipoCuenta="Activo",FechaAlta=now,UsuarioAlta="test"}});
        db.Add(cfg); await db.SaveChangesAsync();
        r.RegimenPercepcionIibbId = await db.PercepcionesIibbEntreRios.Where(x => x.Codigo == "T").Select(x => x.Id).SingleAsync();
        r.CotizacionDolarReferencia = 1450.753m;
        // Open Via1 agreements require hitos even when ordinary installments coexist.
        var openPlan = await RevisionOperationTests.Seed(create, 2);
        await using (var setup = create())
        {
            var via = await setup.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == openPlan.AcuerdoComercialViaId);
            via.ViaOperacion = BudgetControl.Api.Models.Commercial.ViaOperacion.Via1;
            via.ModalidadCobro = BudgetControl.Api.Models.Commercial.ModalidadCobro.Abierta;
            via.AcuerdoComercial.ClienteExternoId = "1234"; via.AcuerdoComercial.ObraExternaId = "56";
            setup.PlanesPago.Remove(await setup.PlanesPago.SingleAsync(p => p.Id == openPlan.Id));
            await setup.SaveChangesAsync();
        }
        await using (var rejected = create())
        {
            try { await Service(rejected).CreateVentaAsync(r); throw new Exception("Empty open agreement allowed draft creation"); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Debe crear al menos un hito"))
            { check(true, "API draft creation requires hitos for existing open Via1 agreement"); }
        }
        var coexistPlan = await RevisionOperationTests.Seed(create, 2);
        await using (var setup = create())
        {
            var via = await setup.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == coexistPlan.AcuerdoComercialViaId);
            via.ViaOperacion = BudgetControl.Api.Models.Commercial.ViaOperacion.Via1;
            via.AcuerdoComercial.ClienteExternoId = "1234"; via.AcuerdoComercial.ObraExternaId = "56";
            await setup.SaveChangesAsync();
        }
        await using (var rejected = create())
        {
            try { await Service(rejected).RegistrarSimpleAsync(r); throw new Exception("Planned quotas bypassed open hitos requirement"); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Debe crear al menos un hito"))
            { check(true, "simple invoice blocked without open hitos even with planned quotas"); }
        }
        check(!await db.Ventas.AnyAsync(v => v.ClienteExternoId == "1234"), "missing-hito rejection leaves no invoice or accounting effects");
        await using (var setup = create())
        {
            var plannedVia = await setup.AcuerdosComercialesVias.SingleAsync(v => v.Id == coexistPlan.AcuerdoComercialViaId);
            plannedVia.Estado = BudgetControl.Api.Models.Commercial.AcuerdoEstado.Finalizado;
            setup.HitosComercialesVias.Add(new() { AcuerdoComercialViaId = openPlan.AcuerdoComercialViaId,
                Descripcion = "Hito requerido", ImporteEstimado = 100, FechaReferencia = now, FechaAlta = now, UsuarioAlta = "test" });
            await setup.SaveChangesAsync();
        }
        // The remaining scenarios below exercise the pre-existing unallocated/plan invoice paths.
        await using (var closeOpen = create())
            await closeOpen.AcuerdosComercialesVias.Where(v => v.Id == openPlan.AcuerdoComercialViaId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Estado, BudgetControl.Api.Models.Commercial.AcuerdoEstado.Finalizado));
        await using var registro = create();
        var venta = await Service(registro).RegistrarSimpleAsync(r);
        check(venta.Total==383 && venta.TotalPercepciones==6 && venta.FiscalSimple?.Lineas.Count==2 && venta.AsientoContableId.HasValue,"register invoice, VAT and IIBB with accounting entry");
        var reread = await Service(db).GetVentaAsync(venta.Id);
        check(reread?.FiscalSimple?.CotizacionDolarReferencia == 1450.753m && reread.Cotizacion == 1 && reread.Total == 383, "USD reference survives reload without converting invoice");
        var mov = await db.Set<VentaMovimientoCuentaCorriente>().SingleAsync(m=>m.ModuloOrigen=="VENTAS" && m.IdOrigen==venta.Id.ToString());
        check(mov.Debe==383,"customer debt equals invoice total");
        var libro=JsonSerializer.SerializeToElement(await Service(db).LibroAsync(now.Date,now.Date));
        var row=libro.EnumerateArray().Single(x=>x.GetProperty("ClienteExternoId").GetString()=="1234");
        check(row.GetProperty("NoGravado").GetDecimal()==10 && row.GetProperty("Devolucion").GetDecimal()==20 && row.GetProperty("Total").GetDecimal()==383,"VAT book separates cheque return and reconciles with debt");
        var alternativo = new PercepcionIibbEntreRios { Codigo="ALT", Descripcion="Alternativo", NumeroRegimen="2", Porcentaje=3, VigenciaDesde=now.Date, UsuarioAlta="test", FechaAlta=now };
        foreach (var concepto in new[] { "VENTA_NO_GRAVADA", "VENTA_EXENTA", "DEVOLUCION_CHEQUES" })
            cfg.Detalles.Add(new() { Concepto = concepto, EsObligatorio = true, TipoMovimiento = "Haber", Orden = cfg.Detalles.Count + 1,
                CuentaContable = new() { Codigo = "T" + cfg.Detalles.Count, Nombre = concepto, TipoCuenta = "Activo", FechaAlta = now, UsuarioAlta = "test" } });
        db.Add(alternativo);
        await db.SaveChangesAsync();
        r.NumeroComprobante = 2;
        r.RegimenPercepcionIibbId = alternativo.Id;
        await using var otroRegistro = create();
        var segunda = await Service(otroRegistro).RegistrarSimpleAsync(r);
        check(segunda.TotalPercepciones == 9 && segunda.Total == 386, "selected invoice regime takes precedence over client default");
        var partidas = await db.Set<AsientoContableDetalle>().Where(d => d.AsientoContableId == segunda.AsientoContableId).ToListAsync();
        var haber = partidas.ToDictionary(d => d.Descripcion.Split(" - ")[0], d => d.Haber);
        check(haber["VENTA_NETA"] == 300 && haber["VENTA_NO_GRAVADA"] == 10 && haber["VENTA_EXENTA"] == 5 &&
            haber["DEVOLUCION_CHEQUES"] == 20 && haber["IVA_DEBITO"] == 42 && haber["PERCEPCION_IIBB"] == 9 &&
            partidas.Sum(d => d.Debe) == 386 && partidas.Sum(d => d.Haber) == 386, "invoice posts each fiscal component exactly once");
        r.NumeroComprobante = 3;
        r.DevolucionCheques = r.NoGravado = r.Exento = 0;
        r.RegimenPercepcionIibbId = null;
        await using var manual = create();
        var tercera = await Service(manual).RegistrarSimpleAsync(r);
        var percepcion = await db.Set<VentaPercepcionIibb>().SingleAsync(x => x.VentaId == tercera.Id && x.Activa);
        check(tercera.Total == 342m && tercera.TotalPercepciones == 0 && percepcion.Importe == 0,
            "no selected rate means no IIBB even for a customer with a configured rate");
        await using var cobros = create();
        var usuario = new CurrentUserService(RevisionOperationTests.Identity());
        var configuraciones = new ConfiguracionesContablesService(cobros, usuario);
        var asientos = new AsientosContablesService(cobros, usuario);
        var automaticos = new ContabilizacionAutomaticaService(cobros, configuraciones, asientos);
        var cartera = new CarteraChequesService(cobros, usuario, automaticos);
        var servicioCobros = new CobranzasService(cobros, usuario, new Clientes(), automaticos, asientos, configuraciones, cartera);
        var banco = await cobros.BancosCobranza.FirstAsync(b => b.Activo);
        var medioCheque = await cobros.MediosPagoCobranza.SingleAsync(m => m.Codigo == "CHEQUE");
        var cobro = await servicioCobros.CreateCobranzaAsync(new() { ClienteExternoId = "1234", Fecha = now, ImporteTotal = 100 });
        await servicioCobros.AddMedioPagoAsync(cobro.Id, new() { MedioPagoCobranzaId = medioCheque.Id, BancoCobranzaId = banco.Id,
            Importe = 100, FechaEmision = now, FechaValor = now });
        var entidad = await cobros.Cobranzas.Include(c => c.MediosPago).ThenInclude(m => m.MedioPago).SingleAsync(c => c.Id == cobro.Id);
        await cartera.EnsureChequesDesdeCobranzaConfirmadaAsync(entidad);
        await cobros.SaveChangesAsync();
        await cartera.EnsureChequesDesdeCobranzaConfirmadaAsync(entidad);
        await cobros.SaveChangesAsync();
        var cheque = await cobros.ChequesTerceros.SingleAsync(c => c.CobranzaMedioPagoId == entidad.MediosPago.Single().Id);
        check(cheque.NumeroCheque == "" && cheque.Librador == "" && cheque.CuitLibrador == "",
            "cheque accepts optional identity fields and enters portfolio once");
        var retencion = await cobros.MediosPagoCobranza.SingleAsync(m => m.Codigo == "RETENCION_IVA");
        var fechaCobro = new DateTime(2026, 11, 4);
        var otroCobro = await servicioCobros.CreateCobranzaAsync(new() { ClienteExternoId = "1234", Fecha = fechaCobro, ImporteTotal = 25 });
        var sinReferencia = await servicioCobros.AddMedioPagoAsync(otroCobro.Id, new() { MedioPagoCobranzaId = retencion.Id, Importe = 25 });
        check(sinReferencia.Fecha.Day == 4 && sinReferencia.TotalMedios == 25,
            "collection preserves calendar date and accepts withholding without reference");
        var cuentaExcel = new CuentaCorrienteClienteResponse {
            ClienteId = "1234", ClienteNombre = "=Cliente & prueba",
            SaldosPorMoneda = new() { new() { MonedaCodigo = "ARS", SaldoFinal = 12.50m } },
            Movimientos = new() { new() { Fecha = fechaCobro, NumeroComprobante = "00001-00000002", Debe = 12.50m } }
        };
        using var excel = new System.IO.Compression.ZipArchive(new MemoryStream(CuentaCorrienteExcel.Crear(cuentaExcel, new())));
        var xml = System.Xml.Linq.XDocument.Load(excel.GetEntry("xl/worksheets/sheet3.xml")!.Open());
        System.Xml.Linq.XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        check(excel.GetEntry("xl/workbook.xml") != null && xml.Descendants(ns + "t").Any(t => t.Value == "00001-00000002") &&
            xml.Descendants(ns + "v").Any(v => decimal.Parse(v.Value, System.Globalization.CultureInfo.InvariantCulture) == 12.50m) &&
            !xml.Descendants(ns + "f").Any(), "Excel export preserves invoice zeros and numeric amounts");
        cobros.Add(new CobranzaAplicacionFactura { CobranzaId = otroCobro.Id, VentaId = venta.Id, ImporteAplicado = 25, FechaAlta = now, UsuarioAlta = "test" });
        await cobros.SaveChangesAsync();
        await cobros.Cobranzas.Where(c => c.Id == otroCobro.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.Estado, CobranzaEstado.Confirmada));
        try {
            await servicioCobros.EliminarBorradorAsync(otroCobro.Id);
            throw new Exception("Confirmed collection deleted");
        } catch (InvalidOperationException) { }
        check(await cobros.Cobranzas.AnyAsync(c => c.Id == otroCobro.Id), "confirmed collection cannot be deleted");
        await cobros.Cobranzas.Where(c => c.Id == otroCobro.Id).ExecuteUpdateAsync(c => c.SetProperty(x => x.Estado, CobranzaEstado.Borrador));
        await servicioCobros.EliminarBorradorAsync(otroCobro.Id);
        await servicioCobros.EliminarBorradorAsync(otroCobro.Id);
        check(!await cobros.Cobranzas.AnyAsync(c => c.Id == otroCobro.Id) &&
            !await cobros.CobranzasMediosPago.AnyAsync(m => m.CobranzaId == otroCobro.Id) &&
            !await cobros.Set<CobranzaAplicacionFactura>().AnyAsync(a => a.CobranzaId == otroCobro.Id),
            "draft deletion removes payment methods and invoice reservations and supports retry");

        var planUsd = await RevisionOperationTests.Seed(create, 2);
        await using (var planDb = create())
        {
            var via = await planDb.AcuerdosComercialesVias.Include(v => v.AcuerdoComercial).SingleAsync(v => v.Id == planUsd.AcuerdoComercialViaId);
            via.ViaOperacion = BudgetControl.Api.Models.Commercial.ViaOperacion.Via1;
            via.MonedaCodigo = "USD";
            via.AcuerdoComercial.ClienteExternoId = "1234";
            via.AcuerdoComercial.ObraExternaId = "56";
            await planDb.SaveChangesAsync();
        }
        var cuotaUsd = planUsd.Cuotas.OrderBy(c => c.NumeroCuota).First();
        r.NumeroComprobante = 4;
        r.NoGravado = 10; r.Exento = 5; r.DevolucionCheques = 20;
        r.RegimenPercepcionIibbId = await db.PercepcionesIibbEntreRios.Where(x => x.Codigo == "T").Select(x => x.Id).SingleAsync();
        r.Aplicaciones = new() { new() { ObligacionId = cuotaUsd.Id, ImporteAplicado = 100 } };
        r.CotizacionDolarReferencia = null;
        await using (var sinTasa = create())
        {
            try { await Service(sinTasa).RegistrarSimpleAsync(r); throw new Exception("USD allocation accepted without invoice FX"); }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cotización")) { }
        }
        check(!await db.Ventas.AnyAsync(v => v.NumeroComprobante == 4 && v.PuntoVenta == 321), "missing FX rolls back invoice");
        r.CotizacionDolarReferencia = 3;
        await using var facturaUsdDb = create();
        var disponiblesUsd = await Service(facturaUsdDb).DisponiblesAsync(new Venta { ClienteExternoId = "1234", ObraExternaId = "56", MonedaCodigo = "ARS" });
        check(disponiblesUsd.Any(o => o.ObligacionId == cuotaUsd.Id && o.MonedaCodigo == "USD"), "ARS invoice offers USD plan installments");
        var facturaUsd = await Service(facturaUsdDb).RegistrarSimpleAsync(r);
        var vinculoUsd = await db.VinculacionesFacturaComerciales.SingleAsync(v => v.FacturaExternaId == facturaUsd.Id.ToString());
        check(facturaUsd.Total == 383 && vinculoUsd.ImporteVinculado == 100,
            "ARS invoice allocates only net 300 / saved FX 3 = USD 100, excluding VAT and extras");
        var cfgCobro = new ConfiguracionContable { CodigoOperacion = "COBRANZA_CLIENTE", Descripcion = "Cobro prueba", UsuarioAlta = "test", FechaAlta = now };
        cfgCobro.Detalles.Add(new() { Concepto = "CLIENTES", TipoMovimiento = "Haber", Orden = 2, CuentaContableId = cfg.Detalles.Single(d => d.Concepto == "CLIENTES").CuentaContableId });
        cfgCobro.Detalles.Add(new() { Concepto = "CAJA", TipoMovimiento = "Debe", EsObligatorio = false, Orden = 1, CuentaContable = new() { Codigo = "TESTCAJA", Nombre = "Caja", TipoCuenta = "Activo", FechaAlta = now, UsuarioAlta = "test" } });
        var cuentaCheques = new CuentaContable { Codigo = "TESTCHEQUE", Nombre = "Cheques", TipoCuenta = "Activo", FechaAlta = now, UsuarioAlta = "test" };
        cfgCobro.Detalles.Add(new() { Concepto = "CHEQUES_TERCEROS", TipoMovimiento = "Debe", EsObligatorio = false, Orden = 3, CuentaContable = cuentaCheques });
        var cfgRechazo = new ConfiguracionContable { CodigoOperacion = "RECHAZO_CHEQUE_CLIENTE", Descripcion = "Rechazo prueba", UsuarioAlta = "test", FechaAlta = now };
        cfgRechazo.Detalles.Add(new() { Concepto = "CLIENTES", TipoMovimiento = "Debe", Orden = 1, CuentaContableId = cfg.Detalles.Single(d => d.Concepto == "CLIENTES").CuentaContableId });
        cfgRechazo.Detalles.Add(new() { Concepto = "CHEQUES_TERCEROS", TipoMovimiento = "Haber", Orden = 2, CuentaContable = cuentaCheques });
        db.Add(cfgRechazo);
        db.Add(cfgCobro);
        await db.SaveChangesAsync();
        CobranzasService CobrosService(AppDbContext ctx) {
            var u = new CurrentUserService(RevisionOperationTests.Identity());
            var conf = new ConfiguracionesContablesService(ctx, u);
            var asi = new AsientosContablesService(ctx, u);
            var auto = new ContabilizacionAutomaticaService(ctx, conf, asi);
            return new(ctx, u, new Clientes(), auto, asi, conf, new CarteraChequesService(ctx, u, auto));
        }
        async Task<int> Cobrar(decimal importe, bool conCheque = false) {
            await using var ctx = create();
            var servicio = CobrosService(ctx);
            var efectivo = await ctx.MediosPagoCobranza.SingleAsync(m => m.Codigo == (conCheque ? "CHEQUE" : "EFECTIVO"));
            var cob = await servicio.CreateCobranzaAsync(new() { ClienteExternoId = "1234", Fecha = now, ImporteTotal = importe });
            await servicio.AddMedioPagoAsync(cob.Id, new() { MedioPagoCobranzaId = efectivo.Id, Importe = importe, BancoCobranzaId = conCheque ? banco.Id : null, FechaEmision = now, FechaValor = now });
            await servicio.AddAplicacionFacturaAsync(cob.Id, new() { VentaId = facturaUsd.Id, ImporteAplicado = importe });
            await servicio.ConfirmarCobranzaAsync(cob.Id);
            return cob.Id;
        }
        var primerCobro = await Cobrar(191.50m);
        check(await db.CuotasComerciales.Where(c => c.Id == cuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 50,
            "half gross ARS payment settles half net USD installment");
        var segundoCobro = await Cobrar(191.50m);
        check(await db.CuotasComerciales.Where(c => c.Id == cuotaUsd.Id).Select(c => c.SaldoPendiente).SingleAsync() == 0,
            "full gross invoice payment settles USD installment exactly");
        await using (var anulacion = create()) await CobrosService(anulacion).AnularCobranzaAsync(segundoCobro, new() { Motivo = "Prueba de anulación" });
        check(await db.CuotasComerciales.Where(c => c.Id == cuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 50,
            "cancelling ARS collection restores USD installment balance");
        await Cobrar(191.50m);
        check(await db.CuotasComerciales.Where(c => c.Id == cuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 100,
            "replacement ARS collection settles remaining USD net");

        // A second invoice exercises cumulative cent rounding across three partial payments.
        r.NumeroComprobante = 5;
        r.CotizacionDolarReferencia = 7;
        r.Lineas = new() { new(tasas[0].Id, 700) };
        r.NoGravado = r.Exento = r.DevolucionCheques = 0;
        var segundaCuotaUsd = planUsd.Cuotas.OrderBy(c => c.NumeroCuota).Last();
        r.Aplicaciones = new() { new() { ObligacionId = segundaCuotaUsd.Id, ImporteAplicado = 100 } };
        await using (var otraFactura = create()) facturaUsd = await Service(otraFactura).RegistrarSimpleAsync(r);
        await Cobrar(facturaUsd.Total / 3);
        check(await db.CuotasComerciales.Where(c => c.Id == segundaCuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 33.33m,
            "first third payment rounds USD allocation to cents");
        await Cobrar(facturaUsd.Total / 3);
        check(await db.CuotasComerciales.Where(c => c.Id == segundaCuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 66.67m,
            "second partial payment uses cumulative USD rounding");
        var cobroConCheque = await Cobrar(facturaUsd.Total / 3, conCheque: true);
        check(await db.CuotasComerciales.Where(c => c.Id == segundaCuotaUsd.Id).Select(c => c.SaldoPendiente).SingleAsync() == 0,
            "last payment clears USD rounding remainder");
        await using (var revisionUsd = create()) {
            var revisable = await RevisionOperationTests.Service(revisionUsd).PrepararAsync(planUsd.AcuerdoComercialVia.AcuerdoComercialId, planUsd.AcuerdoComercialViaId, planUsd.Id);
            check(revisable.TotalPagadoValido == 200, "commercial plan recognizes ARS collections as USD allocations");
        }

        await using (var rechazoDb = create()) {
            var chequeUsd = await rechazoDb.ChequesTerceros.Include(c => c.CobranzaMedioPago).SingleAsync(c => c.CobranzaMedioPago.CobranzaId == cobroConCheque);
            chequeUsd.Estado = ChequeTerceroEstado.DEPOSITADO;
            await rechazoDb.SaveChangesAsync();
            var usuarioRechazo = new CurrentUserService(RevisionOperationTests.Identity());
            var configuracionRechazo = new ConfiguracionesContablesService(rechazoDb, usuarioRechazo);
            var asientosRechazo = new AsientosContablesService(rechazoDb, usuarioRechazo);
            var autoRechazo = new ContabilizacionAutomaticaService(rechazoDb, configuracionRechazo, asientosRechazo);
            await new CarteraChequesService(rechazoDb, usuarioRechazo, autoRechazo).RechazarAsync(chequeUsd.Id, new() { FechaRechazo = now, Motivo = "Prueba conversión" });
        }
        check(await db.CuotasComerciales.Where(c => c.Id == segundaCuotaUsd.Id).Select(c => c.ImportePagado).SingleAsync() == 66.67m,
            "ARS cheque rejection restores only its USD allocation");
        await Cobrar(facturaUsd.Total / 3);
        check(await db.CuotasComerciales.Where(c => c.Id == segundaCuotaUsd.Id).Select(c => c.SaldoPendiente).SingleAsync() == 0,
            "replacement of rejected ARS cheque settles USD balance");
    }
}

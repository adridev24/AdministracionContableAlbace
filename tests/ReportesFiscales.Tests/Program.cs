using System.IO.Compression;
using System.Text.Json;
using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Fiscal;
using BudgetControl.Api.DTOs.Sales;
using BudgetControl.Api.Migrations;
using BudgetControl.Api.Models.Sales;
using BudgetControl.Api.Models.Fiscal;
using BudgetControl.Api.Models.Collections;
using BudgetControl.Api.Services;
using BudgetControl.Api.Services.Fiscal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

var checks = 0;
void Check(bool ok,string message) { if (!ok) throw new Exception("FAIL: "+message); checks++; Console.WriteLine("PASS: "+message); }
void Falla(Action accion,string message) { try { accion(); } catch (InvalidOperationException) { Check(true,message); return; } throw new Exception("FAIL: "+message); }
async Task FallaAsync(Func<Task> accion,string message) { try { await accion(); } catch (InvalidOperationException) { Check(true,message); return; } throw new Exception("FAIL: "+message); }
Venta Venta(int tipo = 1,long numero = 1) => new() { Id=1,TipoComprobanteVentaId=tipo,TipoComprobante=new() {Codigo=tipo==3?"NOTA_CREDITO_A":"FACTURA_A",Descripcion="Prueba",Signo=tipo==3?-1:1},
    ClienteExternoId="42",ObraExternaId="10",FechaComprobante=new(2026,11,5,0,0,0,DateTimeKind.Utc),PuntoVenta=1,NumeroComprobante=numero,
    MonedaCodigo="ARS",Cotizacion=1,NetoGravado=300,TotalIva=42,TotalExento=5,TotalNoGravado=10,TotalPercepciones=6,Total=363,
    FiscalSimpleJson=JsonSerializer.Serialize(new FiscalRegistrado([new(1,100,21,21),new(2,200,10.5m,21)],10,5,0)),Estado=VentaEstado.Confirmada,
    UsuarioAlta="test",FechaAlta=DateTime.UtcNow };
VentaLibroFiscal Fila(int tipo=1) {
    var r=ReportesFiscalesService.Proyectar(Venta(tipo));
    r.DatosConfirmados=true; r.Fiscal.Documento="30710158211"; r.Fiscal.RazonSocial="José & Compañía"; r.Fiscal.CondicionIva="Responsable inscripto";
    return r;
}
Check(IvaSimpleFormato.CuitValido("30710158211"),"CUIT checksum accepts valid document");
Check(!IvaSimpleFormato.CuitValido("30710158215") && !IvaSimpleFormato.CuitValido("00000000000"),"invalid CUIT rejected");
Falla(()=>ReportesFiscalesService.Periodo("2026-13"),"invalid month rejected");
var row=Fila();
Check(IvaSimpleFormato.Validar(row).Count==0,"mixed VAT/exempt/non-taxed/perceptions validates");
var registros=IvaSimpleFormato.Registros(row);
Check(registros.Cabecera.Length==266 && registros.Alicuotas.Count==2 && registros.Alicuotas.All(x=>x.Length==62),"official record lengths and rates count");
Check(registros.Cabecera.Substring(108,15)=="000000000036300" && registros.Cabecera.Substring(183,15)=="000000000000600","total and IIBB official offsets");
Check(registros.Cabecera.Substring(228,13)=="PES0001000000" && registros.Cabecera[241]=='2',"currency/rate and VAT count offsets");
Check(IvaSimpleFormato.Ansi.GetByteCount(registros.Cabecera)==266,"ANSI accented name retains byte width");
var credit=Fila(3);
Check(credit.Signo==-1 && IvaSimpleFormato.Registros(credit).Cabecera.Substring(108,15)=="000000000036300","credit notes retain positive file amounts and negative book sign");
row.Moneda="USD"; row.Cotizacion=1234.56789m;
Check(IvaSimpleFormato.Registros(row).Cabecera.Substring(228,13)=="DOL1234567890","foreign currency exports original amounts and six decimal FX");
row=Fila(); row.Total++;
Check(IvaSimpleFormato.Validar(row).Any(x=>x.Contains("total")),"header total mismatch blocks export");
row=Fila(); row.Alicuotas[0]=new(17,200,21);
Check(IvaSimpleFormato.Validar(row).Any(x=>x.Contains("Alícuota")),"unsupported VAT rate rejected");
row=Fila(); row.Fiscal.RazonSocial="Cliente 😀";
Check(IvaSimpleFormato.Validar(row).Count>0,"non ANSI names rejected instead of replacement bytes");
row=Fila(); row.DevolucionCheques=10;
Check(IvaSimpleFormato.Validar(row).Any(x=>x.Contains("cheques")),"check returns require explicit fiscal review");
row=Fila(); row.Neto=0;row.Iva=0;row.NoGravado=0;row.Percepciones=0;row.Exento=100;row.Total=100;row.Alicuotas=[];row.Fiscal.CodigoOperacion="E";
Check(IvaSimpleFormato.Validar(row).Count==0 && IvaSimpleFormato.Registros(row).Alicuotas.Single().Substring(43,4)=="0003","pure exempt uses explicit zero VAT detail");
var reporte=new ReporteFiscal {Periodo="2026-11",Ventas=[Fila()]};
var c=ConciliacionFiscalService.Ventas(reporte,registros.Cabecera+"\r\n",string.Join("\r\n",registros.Alicuotas));
Check(c.Coincidentes==1 && c.Diferencias.Count==0,"matching official files reconcile");
c=ConciliacionFiscalService.Ventas(reporte,registros.Cabecera+"\r\n"+registros.Cabecera,string.Join("\r\n",registros.Alicuotas));
Check(c.Diferencias.Any(x=>x.Estado=="Duplicado"),"duplicate references not silently collapsed");
Falla(()=>ConciliacionFiscalService.Ventas(reporte,"short",""),"malformed TXT rejected");
Falla(()=>ConciliacionFiscalService.Ventas(reporte,"20261005"+registros.Cabecera[8..],string.Join("\r\n",registros.Alicuotas)),"out of period reference rejected");
var altered=registros.Cabecera[..108]+"000000000036301"+registros.Cabecera[123..];
Check(ConciliacionFiscalService.Ventas(reporte,altered,string.Join("\r\n",registros.Alicuotas)).Coincidentes==1,"one cent comparison tolerance");
altered=registros.Cabecera[..108]+"000000000036302"+registros.Cabecera[123..];
Check(ConciliacionFiscalService.Ventas(reporte,altered,string.Join("\r\n",registros.Alicuotas)).Diferencias.Count==1,"monetary differences detected");
var certificado=new RetencionFiscal { CuitAgente="30710158211",Certificado="00001",Regimen="1",FechaRetencion=new(2026,11,6),PeriodoComputo="2026-11" };
reporte.Retenciones=[new(1,1,"42",new(2026,11,6),"IVA","ARS",1,10,certificado)];
var csv=ConciliacionFiscalService.CabeceraRetenciones+"\r\nIVA;30710158211;00001;1;;2026-11-06;10,00\r\n";
Check(ConciliacionFiscalService.Retenciones(reporte,csv).Coincidentes==1,"retention matching by tax/agent/certificate/regime/jurisdiction");
Check(ConciliacionFiscalService.Csv([new object?[] {"=HYPERLINK()","a;b","x\"y",-2m}]).Contains("'=HYPERLINK"),"spreadsheet formula injection escaped");
Check(ConciliacionFiscalService.Csv([new object?[] {1234.567891m}]).Contains("1234,567891"),"CSV retains six decimal exchange rate");
var rounding = Fila(3); rounding.Neto=1; rounding.Iva=1; rounding.Cotizacion=1.005m;
var summary = new ReporteFiscal { Ventas=[rounding] };
Check(summary.NetoPesos == -1.01m && summary.IvaPesos == -1.01m,"peso summaries round credit notes away from zero");
Falla(()=>ConciliacionFiscalService.Retenciones(reporte,"bad-header"),"unknown retention CSV rejected");

if(args.Length==1)
{
    using var config=JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
    var b=new NpgsqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
    if(b.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("Integration tests require localhost.");
    var original=b.Database; var database="albace_fiscal_test_"+Guid.NewGuid().ToString("N");
    b.Database="postgres"; b.Pooling=false;
    await using var admin=new NpgsqlConnection(b.ConnectionString); await admin.OpenAsync();
    await using(var create=new NpgsqlCommand($"CREATE DATABASE \"{database}\"",admin)) await create.ExecuteNonQueryAsync();
    b.Database=database;
    AppDbContext Context()=>new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(b.ConnectionString).Options);
    try
    {
        await using var db=Context();
        Check(database!=original,"test database isolated from configured database");
        var initializer=db.GetService<IModelRuntimeInitializer>(); var differ=db.GetService<IMigrationsModelDiffer>(); var generator=db.GetService<IMigrationsSqlGenerator>();
        var old=initializer.Initialize(new FacturasHitosVia1().TargetModel,designTime:true); var migration=new ReportesFiscalesVentas();
        var current=db.GetService<IDesignTimeModel>().Model;
        var target=initializer.Initialize(migration.TargetModel,designTime:true);
        Check(!differ.HasDifferences(target.GetRelationalModel(),current.GetRelationalModel()),"migration matches EF model");
        Check(migration.UpOperations.All(x=>x is CreateTableOperation or CreateIndexOperation),"migration only creates new fiscal tables/indexes");
        foreach(var command in generator.Generate(differ.GetDifferences(null,old.GetRelationalModel()),old)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        foreach(var command in generator.Generate(migration.UpOperations,target)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var venta=Venta();venta.Id=0;venta.TipoComprobante=null!;venta.TotalPercepciones=0;venta.Total=357;
        db.Ventas.Add(venta);
        var borrador=Venta(numero:2);borrador.Id=0;borrador.TipoComprobante=null!;borrador.Estado=VentaEstado.Borrador;db.Ventas.Add(borrador);
        var anulada=Venta(numero:3);anulada.Id=0;anulada.TipoComprobante=null!;anulada.Estado=VentaEstado.Anulada;db.Ventas.Add(anulada);
        var cobro=new Cobranza {ClienteExternoId="42",Fecha=venta.FechaComprobante,UsuarioAlta="test",FechaAlta=DateTime.UtcNow,Estado=CobranzaEstado.Confirmada,ImporteTotal=10};
        cobro.MediosPago.Add(new(){MedioPagoCobranzaId=7,Importe=10,UsuarioAlta="test",FechaAlta=DateTime.UtcNow});db.Add(cobro);
        await db.SaveChangesAsync();db.ChangeTracker.Clear();
        ReportesFiscalesService Service(AppDbContext ctx)=>new(ctx,new TestUser());
        var s=Service(db);
        var r=await s.Consultar("2026-11");
        Check(r.Ventas.Count==1 && r.Retenciones.Count==1 && !r.PuedeExportar,"confirmed only; missing company/fiscal data block export");
        Check(r.Observaciones.Count(x=>x.Mensaje.Contains("excluido"))==2,"draft/cancelled exclusions visible");
        var empresa=await s.GuardarEmpresa(new(){Cuit="30710158211",RazonSocial="Empresa test",DomicilioFiscal="Calle 1"});
        db.ChangeTracker.Clear();
        empresa.Actividad="Test"; await s.GuardarEmpresa(empresa);
        Check((await s.Empresa())!.Actividad=="Test","company roundtrip and microsecond concurrency token");
        await FallaAsync(()=>s.GuardarEmpresa(new(){Cuit="30710158211",RazonSocial="Stale",DomicilioFiscal="Calle 1"}),"stale company update rejected");
        db.ChangeTracker.Clear();
        var fiscal=Fila().Fiscal; await s.GuardarVenta(venta.Id,fiscal);
        r=await s.Consultar("2026-11");
        Check(r.PuedeExportar && r.Observaciones.Any(x=>x.Origen=="Retencion" && x.Nivel=="Error"),"retention errors do not block valid sales export");
        var e=await s.Exportar("2026-11");
        using(var zip=new ZipArchive(new MemoryStream(e.Contenido))) Check(zip.Entries.Count==4 && zip.GetEntry("LIBRO_IVA_DIGITAL_VENTAS_CBTE.txt")!.Length==268,"ZIP contains official TXT, snapshot, and instructions");
        Check((await s.Descargar(e.Id))!.Contenido.SequenceEqual(e.Contenido),"historical export re-download is byte identical");
        Check((await db.Ventas.AsNoTracking().SingleAsync(v=>v.Id==venta.Id)).Total==357 && !db.ChangeTracker.Entries<Venta>().Any(),"source sale not modified or tracked by report workflows");
        var medio=cobro.MediosPago.Single();
        await s.GuardarRetencion(medio.Id,new(){CuitAgente="30710158211",RazonSocialAgente="Agente",Certificado="001",Regimen="1",FechaRetencion=venta.FechaComprobante,PeriodoComputo="2026-12"});
        Check((await s.Consultar("2026-12")).Retenciones.Count==1,"retention appears in its computation period even if collected earlier");
        var vacio=await s.Consultar("2026-10"); Check(vacio.Ventas.Count==0 && !vacio.PuedeExportar,"empty period cannot generate misleading files");
        await FallaAsync(()=>s.Exportar("2026-10"),"empty export blocked server side");
        var antes=await db.Ventas.CountAsync();
        foreach(var command in generator.Generate(migration.DownOperations,old)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        Check(await db.Ventas.CountAsync()==antes,"migration rollback preserves operational data");
    }
    finally
    {
        await using var drop=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin); await drop.ExecuteNonQueryAsync();
    }
}
Console.WriteLine($"PASS: {checks} fiscal checks.");
sealed class TestUser : IUserContext { public string UserName=>"fiscal-test"; public string? UserId=>"1"; }

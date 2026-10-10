using System.Text.Json;
using BudgetControl.Api.Data;
using BudgetControl.Api.Migrations;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

if (args.Length != 1) throw new ArgumentException("Pass appsettings.json to locate LOCAL PostgreSQL. Only a newly created disposable database is modified.");
using var config = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var builder = new NpgsqlConnectionStringBuilder(config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
if (builder.Host is not ("localhost" or "127.0.0.1" or "::1"))
    throw new InvalidOperationException("Tests refuse non-local PostgreSQL servers.");
var originalDatabase = builder.Database;
var testDatabase = "albace_revision_test_" + Guid.NewGuid().ToString("N");
builder.Database = "postgres";
builder.Pooling = false;
await using var admin = new NpgsqlConnection(builder.ConnectionString);
await admin.OpenAsync();
await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{testDatabase}\"", admin)) await create.ExecuteNonQueryAsync();
builder.Database = testDatabase;
var connectionString = builder.ConnectionString;
var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}
AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);
try
{
    await using var db = NewContext();
    Check(db.Database.GetDbConnection().Database == testDatabase && testDatabase != originalDatabase, "dedicated disposable database; configured database untouched");
    var initializer = db.GetService<IModelRuntimeInitializer>();
    var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();
    var differ = db.GetService<IMigrationsModelDiffer>();
    var baseline = initializer.Initialize(new TesoreriaCuentasBancariasAcreditacionCheque().TargetModel, designTime: true);
    var current = db.GetService<IDesignTimeModel>().Model;
    var migration = new RevisionPlanPagoAudit();
    var target = initializer.Initialize(migration.TargetModel, designTime: true);
    Check(!differ.HasDifferences(target.GetRelationalModel(), current.GetRelationalModel()), "migration target matches current EF model");
    var snapshot = initializer.Initialize(db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model, designTime: true);
    Check(!differ.HasDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel()), "snapshot matches current EF model");
    foreach (var type in new[] { typeof(AcuerdoComercialVia), typeof(PlanPago), typeof(CuotaComercial) })
    {
        var property = current.FindEntityType(type)!.FindProperty("Version")!;
        Check(property.IsConcurrencyToken && property.GetColumnName() == "xmin" && property.ValueGenerated == ValueGenerated.OnAddOrUpdate, type.Name + " uses database-generated xmin");
    }
    foreach (var command in sqlGenerator.Generate(differ.GetDifferences(null, baseline.GetRelationalModel()), baseline))
        await db.Database.ExecuteSqlRawAsync(command.CommandText);

    // Seed the previous schema directly: migration models use property-bag entities.
    const int historicalId = 900000;
    await db.Database.ExecuteSqlRawAsync("""
        INSERT INTO acuerdos_comerciales(id,cliente_externo_id,obra_externa_id,numero_acuerdo,fecha_acuerdo,monto_total,estado,via_operacion,fecha_alta,usuario_alta)
            VALUES(900000,'test','test','legacy',now(),100,1,1,now(),'test');
        INSERT INTO acuerdos_comerciales_vias(id,acuerdo_comercial_id,via_operacion,moneda_codigo,monto_original,monto_actual,estado,fecha_alta,usuario_alta,modalidad_cobro)
            VALUES(900000,900000,1,'ARS',100,100,1,now(),'test',0);
        INSERT INTO planes_pago(id,acuerdo_comercial_id,acuerdo_comercial_via_id,tiene_anticipo,monto_anticipo,cantidad_cuotas,fecha_primer_vencimiento,periodicidad)
            VALUES(900000,900000,900000,false,0,1,now(),'Mensual');
        INSERT INTO cuotas_comerciales(id,plan_pago_id,numero_cuota,tipo_cuota,fecha_vencimiento,importe_original,importe_pagado,saldo_pendiente,estado)
            VALUES(900000,900000,1,1,now(),100,0,100,0);
        INSERT INTO pagos_comerciales(id,cliente_externo_id,obra_externa_id,acuerdo_comercial_id,acuerdo_comercial_via_id,fecha_pago,importe_total,medio_pago,estado,moneda_codigo,fecha_alta,usuario_alta,origen_pago,tipo_imputacion)
            VALUES(900000,'test','test',900000,900000,now(),1,'test',2,'ARS',now(),'test',0,4);
        INSERT INTO aplicaciones_pago_comerciales(pago_comercial_id,cuota_comercial_id,importe_aplicado,fecha_aplicacion,usuario_aplicacion,tipo_imputacion)
            VALUES(900000,900000,1,now(),'test',4);
        """);
    var up = sqlGenerator.Generate(migration.UpOperations, target);
    var down = sqlGenerator.Generate(migration.DownOperations, baseline);
    Check(!up.Any(c => c.CommandText.Contains("ADD xmin", StringComparison.OrdinalIgnoreCase)) &&
          !down.Any(c => c.CommandText.Contains("DROP COLUMN xmin", StringComparison.OrdinalIgnoreCase)), "provider does not add/drop PostgreSQL system xmin");
    await using (var tx = await db.Database.BeginTransactionAsync())
    {
        foreach (var command in up) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await tx.CommitAsync();
    }
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    async Task<long> Scalar(string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
    Check(await Scalar($"SELECT count(*) FROM cuotas_comerciales_dependencias_historicas WHERE cuota_comercial_id={historicalId}") == 1, "upgrade backfills cancelled payment dependency");
    await RevisionTests.RunAsync(NewContext, connection, Check);
    await ConcurrencyTests.RunAsync(NewContext, connectionString, Check);
    await RevisionOperationTests.RunAsync(NewContext, connectionString, Check);

    // DDL rollback proves no partially installed schema; data below are disposable fixtures only.
    await using (var tx = await db.Database.BeginTransactionAsync())
    {
        foreach (var command in down) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await tx.RollbackAsync();
    }
    Check(await Scalar("SELECT count(*) FROM pg_trigger WHERE tgname='tr_revision_inmutable'") == 1, "Down rollback restores audit and triggers");
    await using (var tx = await db.Database.BeginTransactionAsync())
    {
        foreach (var command in down) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await tx.CommitAsync();
    }
    Check(await Scalar("SELECT count(*) FROM information_schema.tables WHERE table_name='revisiones_planes_pago'") == 0, "Down removes new schema");
    await using (var tx = await db.Database.BeginTransactionAsync())
    {
        foreach (var command in up) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await tx.CommitAsync();
    }
    Check(await Scalar("SELECT count(*) FROM information_schema.tables WHERE table_name='revisiones_planes_pago'") == 1, "Up works again after Down");
    Console.WriteLine($"{checks} checks passed.");
}
finally
{
    // Exact generated identifier only; never the configured database or another existing database.
    if (!testDatabase.StartsWith("albace_revision_test_", StringComparison.Ordinal) || testDatabase == originalDatabase)
        throw new InvalidOperationException("Unsafe cleanup target.");
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Disposable database removed.");
}

static class Fixtures
{
    public static PlanPago NewPlan()
    {
        var now = DateTime.UtcNow;
        var agreement = new AcuerdoComercial { ClienteExternoId = "revision-test", ObraExternaId = "revision-test", NumeroAcuerdo = Guid.NewGuid().ToString(), FechaAcuerdo = now, FechaAlta = now, UsuarioAlta = "test", MontoTotal = 100, Estado = AcuerdoEstado.Aprobado };
        var via = new AcuerdoComercialVia { AcuerdoComercial = agreement, MontoOriginal = 100, MontoActual = 100, MonedaCodigo = "ARS", Estado = AcuerdoEstado.Aprobado, FechaAlta = now, UsuarioAlta = "test" };
        var plan = new PlanPago { AcuerdoComercialVia = via, CantidadCuotas = 1, Periodicidad = "Mensual", FechaPrimerVencimiento = now };
        plan.Cuotas.Add(new CuotaComercial { NumeroCuota = 1, TipoCuota = TipoCuota.Cuota, FechaVencimiento = now, ImporteOriginal = 100, SaldoPendiente = 100 });
        return plan;
    }

    public static PagoComercial Payment(PlanPago plan, PagoEstado estado = PagoEstado.Registrado) => new() {
        AcuerdoComercial = plan.AcuerdoComercialVia.AcuerdoComercial, AcuerdoComercialVia = plan.AcuerdoComercialVia,
        ClienteExternoId = "revision-test", ObraExternaId = "revision-test", FechaPago = DateTime.UtcNow,
        FechaAlta = DateTime.UtcNow, UsuarioAlta = "test", MonedaCodigo = "ARS", MedioPago = "test", ImporteTotal = 10, Estado = estado
    };

    public static RevisionPlanPago Revision(PlanPago plan) => new() {
        SolicitudId = Guid.NewGuid(), AcuerdoComercialId = plan.AcuerdoComercialVia.AcuerdoComercialId,
        AcuerdoComercialViaId = plan.AcuerdoComercialViaId, PlanPagoId = plan.Id,
        MontoAnterior = 100, MontoNuevo = 80, Diferencia = -20, Comentario = "Reducción de prueba",
        UsuarioId = "1", Usuario = "test", Fecha = DateTime.UtcNow, TipoRevision = TipoRevisionPlanPago.Reduccion, MonedaCodigo = "ARS"
    };

    public static RevisionPlanPagoDetalle Detail(PlanPago plan) => new() {
        PlanPagoId = plan.Id, CuotaComercialId = plan.Cuotas.Single().Id, CuotaOriginalId = plan.Cuotas.Single().Id,
        NumeroCuota = 1, TipoCuota = TipoCuota.Cuota, Operacion = OperacionRevisionPlanPago.Modificada,
        ImporteAnterior = 100, ImporteNuevo = 80, VencimientoAnterior = DateTime.UtcNow, VencimientoNuevo = DateTime.UtcNow,
        EstadoAnterior = CuotaEstado.Pendiente, EstadoNuevo = CuotaEstado.Pendiente, ImportePagadoAnterior = 0, SaldoPendienteAnterior = 100
    };
}

using Microsoft.EntityFrameworkCore;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Accounting;
using BudgetControl.Api.Models.Commercial;
using BudgetControl.Api.Models.Sales;

namespace BudgetControl.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        public DbSet<ClienteReferencia> ClientesReferencia { get; set; } = null!;
        public DbSet<ObraReferencia> ObrasReferencia { get; set; } = null!;
        public DbSet<AcuerdoComercial> AcuerdosComerciales { get; set; } = null!;
        public DbSet<AcuerdoComercialVia> AcuerdosComercialesVias { get; set; } = null!;
        public DbSet<PlanPago> PlanesPago { get; set; } = null!;
        public DbSet<CuotaComercial> CuotasComerciales { get; set; } = null!;
        public DbSet<PagoComercial> PagosComerciales { get; set; } = null!;
        public DbSet<AplicacionPagoComercial> AplicacionesPagoComerciales { get; set; } = null!;
        public DbSet<HitoComercialVia> HitosComercialesVias { get; set; } = null!;
        public DbSet<VinculacionFacturaComercial> VinculacionesFacturaComerciales { get; set; } = null!;
        public DbSet<AjusteCuotaComercial> AjustesCuotaComerciales { get; set; } = null!;
        public DbSet<AjusteAcuerdoComercialVia> AjustesAcuerdosComercialesVias { get; set; } = null!;
        public DbSet<CuentaContable> CuentasContables { get; set; } = null!;
        public DbSet<AsientoContable> AsientosContables { get; set; } = null!;
        public DbSet<AsientoContableDetalle> AsientosContablesDetalle { get; set; } = null!;
        public DbSet<ConfiguracionContable> ConfiguracionesContables { get; set; } = null!;
        public DbSet<ConfiguracionContableDetalle> ConfiguracionesContablesDetalle { get; set; } = null!;
        public DbSet<TipoComprobanteVenta> TiposComprobanteVenta { get; set; } = null!;
        public DbSet<Venta> Ventas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FullName).HasColumnName("full_name");
                entity.Property(u => u.Email).HasColumnName("email");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.Name).HasColumnName("name");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                entity.Property(ur => ur.UserId).HasColumnName("user_id");
                entity.Property(ur => ur.RoleId).HasColumnName("role_id");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            });

            modelBuilder.Entity<ClienteReferencia>(entity =>
            {
                entity.ToTable("clientes_referencia");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id");
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.Documento).HasColumnName("documento");
                entity.Property(e => e.Activo).HasColumnName("activo");
            });

            modelBuilder.Entity<ObraReferencia>(entity =>
            {
                entity.ToTable("obras_referencia");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id");
                entity.Property(e => e.NombreObra).HasColumnName("nombre_obra");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.Activa).HasColumnName("activa");
            });

            modelBuilder.Entity<AcuerdoComercial>(entity =>
            {
                entity.ToTable("acuerdos_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id");
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id");
                entity.Property(e => e.NumeroAcuerdo).HasColumnName("numero_acuerdo");
                entity.Property(e => e.FechaAcuerdo).HasColumnName("fecha_acuerdo");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.MontoTotal).HasColumnName("monto_total");
                entity.Property(e => e.Estado).HasColumnName("estado");
                entity.Property(e => e.ViaOperacion).HasColumnName("via_operacion");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta");
            });

            modelBuilder.Entity<AcuerdoComercialVia>(entity =>
            {
                entity.ToTable("acuerdos_comerciales_vias");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
                entity.Property(e => e.ViaOperacion).HasColumnName("via_operacion");
                entity.Property(e => e.ModalidadCobro)
                    .HasColumnName("modalidad_cobro")
                    .HasDefaultValue(ModalidadCobro.Planificada);
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo");
                entity.Property(e => e.MontoOriginal).HasColumnName("monto_original");
                entity.Property(e => e.MontoActual).HasColumnName("monto_actual");
                entity.Property(e => e.Estado).HasColumnName("estado");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta");

                entity.HasIndex(e => new { e.AcuerdoComercialId, e.ViaOperacion }).IsUnique();

                entity.HasOne(e => e.AcuerdoComercial)
                    .WithMany(a => a.Vias)
                    .HasForeignKey(e => e.AcuerdoComercialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PlanPago>(entity =>
            {
                entity.ToTable("planes_pago");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
                entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
                entity.Property(e => e.TieneAnticipo).HasColumnName("tiene_anticipo");
                entity.Property(e => e.MontoAnticipo).HasColumnName("monto_anticipo");
                entity.Property(e => e.CantidadCuotas).HasColumnName("cantidad_cuotas");
                entity.Property(e => e.FechaPrimerVencimiento).HasColumnName("fecha_primer_vencimiento");
                entity.Property(e => e.Periodicidad).HasColumnName("periodicidad");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");

                entity.HasOne(e => e.AcuerdoComercialVia)
                    .WithOne(v => v.PlanPago)
                    .HasForeignKey<PlanPago>(e => e.AcuerdoComercialViaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CuotaComercial>(entity =>
            {
                entity.ToTable("cuotas_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PlanPagoId).HasColumnName("plan_pago_id");
                entity.Property(e => e.NumeroCuota).HasColumnName("numero_cuota");
                entity.Property(e => e.TipoCuota).HasColumnName("tipo_cuota");
                entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
                entity.Property(e => e.ImporteOriginal).HasColumnName("importe_original");
                entity.Property(e => e.ImportePagado).HasColumnName("importe_pagado");
                entity.Property(e => e.SaldoPendiente).HasColumnName("saldo_pendiente");
                entity.Property(e => e.Estado).HasColumnName("estado");

                entity.HasOne(e => e.PlanPago)
                    .WithMany(p => p.Cuotas)
                    .HasForeignKey(e => e.PlanPagoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PagoComercial>(entity =>
            {
                entity.ToTable("pagos_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id");
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id");
                entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
                entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
                entity.Property(e => e.FechaPago).HasColumnName("fecha_pago");
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo");
                entity.Property(e => e.ImporteTotal).HasColumnName("importe_total");
                entity.Property(e => e.MedioPago).HasColumnName("medio_pago");
                entity.Property(e => e.TipoImputacion)
                    .HasColumnName("tipo_imputacion")
                    .HasDefaultValue(TipoImputacion.SaldoGeneral);
                entity.Property(e => e.OrigenPago)
                    .HasColumnName("origen_pago")
                    .HasDefaultValue(OrigenPago.Comercial);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.Estado).HasColumnName("estado");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta");

                entity.HasOne(e => e.AcuerdoComercial)
                    .WithMany(a => a.Pagos)
                    .HasForeignKey(e => e.AcuerdoComercialId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AcuerdoComercialVia)
                    .WithMany(v => v.Pagos)
                    .HasForeignKey(e => e.AcuerdoComercialViaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AplicacionPagoComercial>(entity =>
            {
                entity.ToTable("aplicaciones_pago_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PagoComercialId).HasColumnName("pago_comercial_id");
                entity.Property(e => e.CuotaComercialId).HasColumnName("cuota_comercial_id");
                entity.Property(e => e.HitoComercialViaId).HasColumnName("hito_comercial_via_id");
                entity.Property(e => e.ImporteAplicado).HasColumnName("importe_aplicado");
                entity.Property(e => e.FechaAplicacion).HasColumnName("fecha_aplicacion");
                entity.Property(e => e.TipoImputacion)
                    .HasColumnName("tipo_imputacion")
                    .HasDefaultValue(TipoImputacion.Cuota);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.UsuarioAplicacion).HasColumnName("usuario_aplicacion");

                entity.HasOne(e => e.PagoComercial)
                    .WithMany(p => p.Aplicaciones)
                    .HasForeignKey(e => e.PagoComercialId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CuotaComercial)
                    .WithMany(c => c.Aplicaciones)
                    .HasForeignKey(e => e.CuotaComercialId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.HitoComercialVia)
                    .WithMany(h => h.Aplicaciones)
                    .HasForeignKey(e => e.HitoComercialViaId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<HitoComercialVia>(entity =>
            {
                entity.ToTable("hitos_comerciales_vias");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.ImporteEstimado).HasColumnName("importe_estimado");
                entity.Property(e => e.FechaReferencia).HasColumnName("fecha_referencia");
                entity.Property(e => e.ImporteAplicado).HasColumnName("importe_aplicado");
                entity.Property(e => e.Estado).HasColumnName("estado");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta");

                entity.HasOne(e => e.AcuerdoComercialVia)
                    .WithMany(v => v.Hitos)
                    .HasForeignKey(e => e.AcuerdoComercialViaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VinculacionFacturaComercial>(entity =>
            {
                entity.ToTable("vinculaciones_factura_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CuotaComercialId).HasColumnName("cuota_comercial_id");
                entity.Property(e => e.FacturaExternaId).HasColumnName("factura_externa_id");
                entity.Property(e => e.NumeroFactura).HasColumnName("numero_factura");
                entity.Property(e => e.ImporteVinculado).HasColumnName("importe_vinculado");
                entity.Property(e => e.FechaVinculacion).HasColumnName("fecha_vinculacion");

                entity.HasOne(e => e.CuotaComercial)
                    .WithMany(c => c.VinculacionesFactura)
                    .HasForeignKey(e => e.CuotaComercialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AjusteCuotaComercial>(entity =>
            {
                entity.ToTable("ajustes_cuotas_comerciales");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CuotaComercialId).HasColumnName("cuota_comercial_id");
                entity.Property(e => e.PlanPagoId).HasColumnName("plan_pago_id");
                entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
                entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
                entity.Property(e => e.TipoAjuste).HasColumnName("tipo_ajuste");
                entity.Property(e => e.ImporteAnterior).HasColumnName("importe_anterior");
                entity.Property(e => e.ImporteNuevo).HasColumnName("importe_nuevo");
                entity.Property(e => e.FechaVencimientoAnterior).HasColumnName("fecha_vencimiento_anterior");
                entity.Property(e => e.FechaVencimientoNueva).HasColumnName("fecha_vencimiento_nueva");
                entity.Property(e => e.Motivo).HasColumnName("motivo");
                entity.Property(e => e.FechaAjuste).HasColumnName("fecha_ajuste");
                entity.Property(e => e.UsuarioAjuste).HasColumnName("usuario_ajuste");

                entity.HasOne(e => e.CuotaComercial)
                    .WithMany(c => c.Ajustes)
                    .HasForeignKey(e => e.CuotaComercialId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PlanPago)
                    .WithMany()
                    .HasForeignKey(e => e.PlanPagoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AcuerdoComercialVia)
                    .WithMany()
                    .HasForeignKey(e => e.AcuerdoComercialViaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AcuerdoComercial)
                    .WithMany()
                    .HasForeignKey(e => e.AcuerdoComercialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AjusteAcuerdoComercialVia>(entity =>
            {
                entity.ToTable("ajustes_acuerdos_comerciales_vias");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AcuerdoComercialViaId).HasColumnName("acuerdo_comercial_via_id");
                entity.Property(e => e.AcuerdoComercialId).HasColumnName("acuerdo_comercial_id");
                entity.Property(e => e.ViaOperacion).HasColumnName("via_operacion");
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo");
                entity.Property(e => e.MontoAnterior).HasColumnName("monto_anterior");
                entity.Property(e => e.MontoNuevo).HasColumnName("monto_nuevo");
                entity.Property(e => e.Diferencia).HasColumnName("diferencia");
                entity.Property(e => e.TipoAjuste).HasColumnName("tipo_ajuste");
                entity.Property(e => e.Motivo).HasColumnName("motivo");
                entity.Property(e => e.FechaAjuste).HasColumnName("fecha_ajuste");
                entity.Property(e => e.UsuarioAjuste).HasColumnName("usuario_ajuste");

                entity.HasOne(e => e.AcuerdoComercialVia)
                    .WithMany(v => v.Ajustes)
                    .HasForeignKey(e => e.AcuerdoComercialViaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AcuerdoComercial)
                    .WithMany()
                    .HasForeignKey(e => e.AcuerdoComercialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CuentaContable>(entity =>
            {
                entity.ToTable("cuentas_contables");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
                entity.Property(e => e.TipoCuenta).HasColumnName("tipo_cuenta").HasMaxLength(30).IsRequired();
                entity.Property(e => e.Activa).HasColumnName("activa").HasDefaultValue(true);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_cuentas_contables_codigo")
                    .IsUnique();
            });

            modelBuilder.Entity<AsientoContable>(entity =>
            {
                entity.ToTable("asientos_contables");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired();
                entity.Property(e => e.ModuloOrigen).HasColumnName("modulo_origen").HasMaxLength(100);
                entity.Property(e => e.IdOrigen).HasColumnName("id_origen").HasMaxLength(100);
                entity.Property(e => e.EsAutomatico).HasColumnName("es_automatico").HasDefaultValue(false);
                entity.Property(e => e.EsReversion).HasColumnName("es_reversion").HasDefaultValue(false);
                entity.Property(e => e.IdAsientoRevertido).HasColumnName("id_asiento_revertido");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.IdAsientoRevertido)
                    .HasDatabaseName("ix_asientos_contables_id_asiento_revertido");

                entity.HasIndex(e => new { e.ModuloOrigen, e.IdOrigen })
                    .HasDatabaseName("ix_asientos_contables_modulo_origen_id_origen");

                entity.HasOne(e => e.AsientoRevertido)
                    .WithMany(e => e.Reversiones)
                    .HasForeignKey(e => e.IdAsientoRevertido)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AsientoContableDetalle>(entity =>
            {
                entity.ToTable("asientos_contables_detalle");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.AsientoContableId).HasColumnName("asiento_contable_id");
                entity.Property(e => e.CuentaContableId).HasColumnName("cuenta_contable_id");
                entity.Property(e => e.Debe).HasColumnName("debe").HasPrecision(18, 2);
                entity.Property(e => e.Haber).HasColumnName("haber").HasPrecision(18, 2);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired();

                entity.HasIndex(e => e.AsientoContableId)
                    .HasDatabaseName("ix_asientos_contables_detalle_asiento_contable_id");

                entity.HasIndex(e => e.CuentaContableId)
                    .HasDatabaseName("ix_asientos_contables_detalle_cuenta_contable_id");

                entity.HasOne(e => e.AsientoContable)
                    .WithMany(e => e.Detalles)
                    .HasForeignKey(e => e.AsientoContableId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CuentaContable)
                    .WithMany()
                    .HasForeignKey(e => e.CuentaContableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ConfiguracionContable>(entity =>
            {
                entity.ToTable("configuraciones_contables");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.CodigoOperacion).HasColumnName("codigo_operacion").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired();
                entity.Property(e => e.Activa).HasColumnName("activa").HasDefaultValue(true);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.CodigoOperacion)
                    .HasDatabaseName("ix_configuraciones_contables_codigo_operacion")
                    .IsUnique();
            });

            modelBuilder.Entity<ConfiguracionContableDetalle>(entity =>
            {
                entity.ToTable("configuraciones_contables_detalle");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.ConfiguracionContableId).HasColumnName("configuracion_contable_id");
                entity.Property(e => e.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(10).IsRequired();
                entity.Property(e => e.Concepto).HasColumnName("concepto").HasMaxLength(100).IsRequired();
                entity.Property(e => e.CuentaContableId).HasColumnName("cuenta_contable_id");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);

                entity.HasIndex(e => e.ConfiguracionContableId)
                    .HasDatabaseName("ix_configuraciones_contables_detalle_configuracion_contable_id");

                entity.HasIndex(e => e.CuentaContableId)
                    .HasDatabaseName("ix_configuraciones_contables_detalle_cuenta_contable_id");

                entity.HasIndex(e => new { e.ConfiguracionContableId, e.Concepto })
                    .HasDatabaseName("ix_configuraciones_contables_detalle_concepto_activo")
                    .IsUnique()
                    .HasFilter("activo = true");

                entity.HasOne(e => e.ConfiguracionContable)
                    .WithMany(e => e.Detalles)
                    .HasForeignKey(e => e.ConfiguracionContableId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CuentaContable)
                    .WithMany()
                    .HasForeignKey(e => e.CuentaContableId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TipoComprobanteVenta>(entity =>
            {
                entity.ToTable("tipos_comprobante_venta");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Letra).HasColumnName("letra").HasMaxLength(5);
                entity.Property(e => e.Signo).HasColumnName("signo").HasDefaultValue(1);
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.Orden).HasColumnName("orden");

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_tipos_comprobante_venta_codigo")
                    .IsUnique();

                entity.HasData(
                    new TipoComprobanteVenta { Id = 1, Codigo = "FACTURA_A", Descripcion = "Factura A", Letra = "A", Signo = 1, Activo = true, Orden = 10 },
                    new TipoComprobanteVenta { Id = 2, Codigo = "FACTURA_B", Descripcion = "Factura B", Letra = "B", Signo = 1, Activo = true, Orden = 20 },
                    new TipoComprobanteVenta { Id = 3, Codigo = "FACTURA_C", Descripcion = "Factura C", Letra = "C", Signo = 1, Activo = true, Orden = 30 },
                    new TipoComprobanteVenta { Id = 4, Codigo = "NOTA_DEBITO", Descripcion = "Nota de debito", Letra = null, Signo = 1, Activo = true, Orden = 40 },
                    new TipoComprobanteVenta { Id = 5, Codigo = "NOTA_CREDITO", Descripcion = "Nota de credito", Letra = null, Signo = -1, Activo = true, Orden = 50 }
                );
            });

            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("ventas");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.TipoComprobanteVentaId).HasColumnName("tipo_comprobante_venta_id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.FechaComprobante).HasColumnName("fecha_comprobante");
                entity.Property(e => e.PuntoVenta).HasColumnName("punto_venta");
                entity.Property(e => e.NumeroComprobante).HasColumnName("numero_comprobante");
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasMaxLength(10).IsRequired();
                entity.Property(e => e.Cotizacion).HasColumnName("cotizacion").HasPrecision(18, 6);
                entity.Property(e => e.Estado).HasColumnName("estado").HasDefaultValue(VentaEstado.Borrador);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.FechaComprobante)
                    .HasDatabaseName("ix_ventas_fecha_comprobante");

                entity.HasIndex(e => e.ClienteExternoId)
                    .HasDatabaseName("ix_ventas_cliente_externo_id");

                entity.HasIndex(e => e.ObraExternaId)
                    .HasDatabaseName("ix_ventas_obra_externa_id");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("ix_ventas_estado");

                entity.HasIndex(e => new { e.TipoComprobanteVentaId, e.PuntoVenta, e.NumeroComprobante })
                    .HasDatabaseName("ix_ventas_numeracion")
                    .IsUnique();

                entity.HasOne(e => e.TipoComprobante)
                    .WithMany(t => t.Ventas)
                    .HasForeignKey(e => e.TipoComprobanteVentaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

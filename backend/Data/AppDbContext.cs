using Microsoft.EntityFrameworkCore;
using BudgetControl.Api.Models;
using BudgetControl.Api.Models.Accounting;
using BudgetControl.Api.Models.Collections;
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
        public DbSet<VentaDetalle> VentasDetalle { get; set; } = null!;
        public DbSet<PuntoVenta> PuntosVenta { get; set; } = null!;
        public DbSet<PuntoVentaComprobante> PuntosVentaComprobantes { get; set; } = null!;
        public DbSet<AlicuotaIvaVenta> AlicuotasIvaVenta { get; set; } = null!;
        public DbSet<NomencladorFce> NomencladoresFce { get; set; } = null!;
        public DbSet<PercepcionIibbEntreRios> PercepcionesIibbEntreRios { get; set; } = null!;
        public DbSet<ClientePercepcionIibbConfig> ClientesPercepcionIibbConfig { get; set; } = null!;
        public DbSet<VentaPercepcionIibb> VentasPercepcionesIibb { get; set; } = null!;
        public DbSet<VentaMovimientoCuentaCorriente> VentasMovimientosCuentaCorriente { get; set; } = null!;
        public DbSet<CategoriaItemFacturable> CategoriasItemsFacturables { get; set; } = null!;
        public DbSet<UnidadMedidaVenta> UnidadesMedidaVenta { get; set; } = null!;
        public DbSet<ItemFacturable> ItemsFacturables { get; set; } = null!;
        public DbSet<Cobranza> Cobranzas { get; set; } = null!;
        public DbSet<MedioPagoCobranza> MediosPagoCobranza { get; set; } = null!;
        public DbSet<BancoCobranza> BancosCobranza { get; set; } = null!;
        public DbSet<CobranzaMedioPago> CobranzasMediosPago { get; set; } = null!;
        public DbSet<CobranzaAplicacionFactura> CobranzasAplicacionesFactura { get; set; } = null!;
        public DbSet<CobranzaAplicacionObligacion> CobranzasAplicacionesObligacion { get; set; } = null!;

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
                entity.Property(e => e.FechaAnulacion).HasColumnName("fecha_anulacion");
                entity.Property(e => e.UsuarioAnulacion).HasColumnName("usuario_anulacion");
                entity.Property(e => e.MotivoAnulacion).HasColumnName("motivo_anulacion");

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
                entity.Property(e => e.EsObligatorio).HasColumnName("es_obligatorio");
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
                entity.Property(e => e.TipoFiscal).HasColumnName("tipo_fiscal").HasMaxLength(50).HasDefaultValue("Local").IsRequired();
                entity.Property(e => e.EsCreditoElectronica).HasColumnName("es_credito_electronica").HasDefaultValue(false);
                entity.Property(e => e.EsExportacion).HasColumnName("es_exportacion").HasDefaultValue(false);
                entity.Property(e => e.RequiereNomenclador).HasColumnName("requiere_nomenclador").HasDefaultValue(false);
                entity.Property(e => e.PermiteIva).HasColumnName("permite_iva").HasDefaultValue(true);
                entity.Property(e => e.Signo).HasColumnName("signo").HasDefaultValue(1);
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).HasDefaultValue("Sistema").IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_tipos_comprobante_venta_codigo")
                    .IsUnique();

                entity.HasData(
                    BuildTipoComprobanteSeed(1, "FACTURA_A", "Factura A", "A", "Local", false, false, false, true, 1, true, 10),
                    BuildTipoComprobanteSeed(2, "FACTURA_B", "Factura B", "B", "Local", false, false, false, true, 1, true, 20),
                    BuildTipoComprobanteSeed(3, "FACTURA_C", "Factura C", "C", "Local", false, false, false, true, 1, true, 30),
                    BuildTipoComprobanteSeed(4, "NOTA_DEBITO", "Nota de debito", null, "Local", false, false, false, true, 1, true, 40),
                    BuildTipoComprobanteSeed(5, "NOTA_CREDITO", "Nota de credito", null, "Local", false, false, false, true, -1, true, 50),
                    BuildTipoComprobanteSeed(6, "FACTURA_E", "Factura E", "E", "Exportacion", false, true, false, false, 1, true, 60),
                    BuildTipoComprobanteSeed(7, "FCE_MIPYME_A_CON_NOMENCLADOR", "Factura de Credito Electronica MiPyME A con nomenclador", "A", "Local", true, false, true, true, 1, true, 70),
                    BuildTipoComprobanteSeed(8, "FCE_MIPYME_A_SIN_NOMENCLADOR", "Factura de Credito Electronica MiPyME A sin nomenclador", "A", "Local", true, false, false, true, 1, true, 80),
                    BuildTipoComprobanteSeed(9, "NOTA_DEBITO_A", "Nota de debito A", "A", "Local", false, false, false, true, 1, true, 90),
                    BuildTipoComprobanteSeed(10, "NOTA_CREDITO_A", "Nota de credito A", "A", "Local", false, false, false, true, -1, true, 100),
                    BuildTipoComprobanteSeed(11, "NOTA_DEBITO_B", "Nota de debito B", "B", "Local", false, false, false, true, 1, true, 110),
                    BuildTipoComprobanteSeed(12, "NOTA_CREDITO_B", "Nota de credito B", "B", "Local", false, false, false, true, -1, true, 120),
                    BuildTipoComprobanteSeed(13, "NOTA_DEBITO_E", "Nota de debito E", "E", "Exportacion", false, true, false, false, 1, true, 130),
                    BuildTipoComprobanteSeed(14, "NOTA_CREDITO_E", "Nota de credito E", "E", "Exportacion", false, true, false, false, -1, true, 140),
                    BuildTipoComprobanteSeed(15, "FCE_MIPYME_NOTA_DEBITO_A_CON_NOMENCLADOR", "Nota de debito FCE MiPyME A con nomenclador", "A", "Local", true, false, true, true, 1, true, 150),
                    BuildTipoComprobanteSeed(16, "FCE_MIPYME_NOTA_CREDITO_A_CON_NOMENCLADOR", "Nota de credito FCE MiPyME A con nomenclador", "A", "Local", true, false, true, true, -1, true, 160),
                    BuildTipoComprobanteSeed(17, "FCE_MIPYME_NOTA_DEBITO_A_SIN_NOMENCLADOR", "Nota de debito FCE MiPyME A sin nomenclador", "A", "Local", true, false, false, true, 1, true, 170),
                    BuildTipoComprobanteSeed(18, "FCE_MIPYME_NOTA_CREDITO_A_SIN_NOMENCLADOR", "Nota de credito FCE MiPyME A sin nomenclador", "A", "Local", true, false, false, true, -1, true, 180)
                );
            });

            modelBuilder.Entity<PuntoVenta>(entity =>
            {
                entity.ToTable("puntos_venta");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Numero).HasColumnName("numero");
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Numero)
                    .HasDatabaseName("ix_puntos_venta_numero")
                    .IsUnique();
            });

            modelBuilder.Entity<PuntoVentaComprobante>(entity =>
            {
                entity.ToTable("puntos_venta_comprobantes");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.PuntoVentaId).HasColumnName("punto_venta_id");
                entity.Property(e => e.TipoComprobanteVentaId).HasColumnName("tipo_comprobante_venta_id");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => new { e.PuntoVentaId, e.TipoComprobanteVentaId })
                    .HasDatabaseName("ix_puntos_venta_comprobantes_punto_tipo")
                    .IsUnique();

                entity.HasOne(e => e.PuntoVenta)
                    .WithMany(p => p.Comprobantes)
                    .HasForeignKey(e => e.PuntoVentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TipoComprobante)
                    .WithMany(t => t.PuntosVentaComprobantes)
                    .HasForeignKey(e => e.TipoComprobanteVentaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Venta>(entity =>
            {
                entity.ToTable("ventas");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.TipoComprobanteVentaId).HasColumnName("tipo_comprobante_venta_id");
                entity.Property(e => e.PuntoVentaComprobanteId).HasColumnName("punto_venta_comprobante_id");
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.FechaComprobante).HasColumnName("fecha_comprobante");
                entity.Property(e => e.PuntoVenta).HasColumnName("punto_venta");
                entity.Property(e => e.NumeroComprobante).HasColumnName("numero_comprobante");
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasMaxLength(10).IsRequired();
                entity.Property(e => e.Cotizacion).HasColumnName("cotizacion").HasPrecision(18, 6);
                entity.Property(e => e.SubtotalBruto).HasColumnName("subtotal_bruto").HasPrecision(18, 2);
                entity.Property(e => e.TotalDescuentos).HasColumnName("total_descuentos").HasPrecision(18, 2);
                entity.Property(e => e.NetoGravado).HasColumnName("neto_gravado").HasPrecision(18, 2);
                entity.Property(e => e.TotalExento).HasColumnName("total_exento").HasPrecision(18, 2);
                entity.Property(e => e.TotalNoGravado).HasColumnName("total_no_gravado").HasPrecision(18, 2);
                entity.Property(e => e.TotalIva).HasColumnName("total_iva").HasPrecision(18, 2);
                entity.Property(e => e.TotalAntesPercepciones).HasColumnName("total_antes_percepciones").HasPrecision(18, 2);
                entity.Property(e => e.TotalPercepciones).HasColumnName("total_percepciones").HasPrecision(18, 2);
                entity.Property(e => e.Total).HasColumnName("total").HasPrecision(18, 2);
                entity.Property(e => e.PercepcionIibbRequiereRecalculo).HasColumnName("percepcion_iibb_requiere_recalculo");
                entity.Property(e => e.FechaUltimoCalculoPercepcion).HasColumnName("fecha_ultimo_calculo_percepcion");
                entity.Property(e => e.Estado).HasColumnName("estado").HasDefaultValue(VentaEstado.Borrador);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);
                entity.Property(e => e.FechaConfirmacion).HasColumnName("fecha_confirmacion");
                entity.Property(e => e.UsuarioConfirmacion).HasColumnName("usuario_confirmacion").HasMaxLength(100);
                entity.Property(e => e.AsientoContableId).HasColumnName("asiento_contable_id");

                entity.HasIndex(e => e.FechaComprobante)
                    .HasDatabaseName("ix_ventas_fecha_comprobante");

                entity.HasIndex(e => e.ClienteExternoId)
                    .HasDatabaseName("ix_ventas_cliente_externo_id");

                entity.HasIndex(e => e.ObraExternaId)
                    .HasDatabaseName("ix_ventas_obra_externa_id");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("ix_ventas_estado");

                entity.HasIndex(e => e.AsientoContableId)
                    .HasDatabaseName("ix_ventas_asiento_contable_id");

                entity.HasIndex(e => new { e.TipoComprobanteVentaId, e.PuntoVenta, e.NumeroComprobante })
                    .HasDatabaseName("ix_ventas_numeracion")
                    .IsUnique();

                entity.HasIndex(e => e.PuntoVentaComprobanteId)
                    .HasDatabaseName("ix_ventas_punto_venta_comprobante_id");

                entity.HasOne(e => e.TipoComprobante)
                    .WithMany(t => t.Ventas)
                    .HasForeignKey(e => e.TipoComprobanteVentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PuntoVentaComprobante)
                    .WithMany(r => r.Ventas)
                    .HasForeignKey(e => e.PuntoVentaComprobanteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VentaMovimientoCuentaCorriente>(entity =>
            {
                entity.ToTable("ventas_movimientos_cuenta_corriente");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.ObraExternaId).HasColumnName("obra_externa_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Debe).HasColumnName("debe").HasPrecision(18, 2);
                entity.Property(e => e.Haber).HasColumnName("haber").HasPrecision(18, 2);
                entity.Property(e => e.ModuloOrigen).HasColumnName("modulo_origen").HasMaxLength(50).IsRequired();
                entity.Property(e => e.IdOrigen).HasColumnName("id_origen").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.ClienteExternoId)
                    .HasDatabaseName("ix_ventas_mov_cc_cliente");

                entity.HasIndex(e => e.ObraExternaId)
                    .HasDatabaseName("ix_ventas_mov_cc_obra");

                entity.HasIndex(e => new { e.ModuloOrigen, e.IdOrigen, e.TipoMovimiento })
                    .HasDatabaseName("ix_ventas_mov_cc_origen_tipo")
                    .IsUnique();
            });

            modelBuilder.Entity<ClientePercepcionIibbConfig>(entity =>
            {
                entity.ToTable("ventas_clientes_percepcion_iibb_config");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Situacion).HasColumnName("situacion");
                entity.Property(e => e.RegimenPercepcionIibbId).HasColumnName("regimen_percepcion_iibb_id");
                entity.Property(e => e.NumeroInscripcionIibb).HasColumnName("numero_inscripcion_iibb").HasMaxLength(50);
                entity.Property(e => e.JurisdiccionIibb).HasColumnName("jurisdiccion_iibb").HasMaxLength(100);
                entity.Property(e => e.ExclusionDesde).HasColumnName("exclusion_desde");
                entity.Property(e => e.ExclusionHasta).HasColumnName("exclusion_hasta");
                entity.Property(e => e.MotivoExclusion).HasColumnName("motivo_exclusion").HasMaxLength(500);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.ClienteExternoId)
                    .HasDatabaseName("ix_ventas_clientes_percepcion_iibb_cliente")
                    .IsUnique();

                entity.HasIndex(e => e.RegimenPercepcionIibbId)
                    .HasDatabaseName("ix_ventas_clientes_percepcion_iibb_regimen");

                entity.HasOne(e => e.RegimenPercepcionIibb)
                    .WithMany()
                    .HasForeignKey(e => e.RegimenPercepcionIibbId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VentaPercepcionIibb>(entity =>
            {
                entity.ToTable("ventas_percepciones_iibb_aplicadas");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.VentaId).HasColumnName("venta_id");
                entity.Property(e => e.RegimenPercepcionIibbId).HasColumnName("regimen_percepcion_iibb_id");
                entity.Property(e => e.CodigoRegimenAplicado).HasColumnName("codigo_regimen_aplicado").HasMaxLength(50);
                entity.Property(e => e.DescripcionRegimenAplicada).HasColumnName("descripcion_regimen_aplicada").HasMaxLength(250);
                entity.Property(e => e.JurisdiccionAplicada).HasColumnName("jurisdiccion_aplicada").HasMaxLength(100);
                entity.Property(e => e.TipoTributoAplicado).HasColumnName("tipo_tributo_aplicado").HasMaxLength(50);
                entity.Property(e => e.NumeroRegimenAplicado).HasColumnName("numero_regimen_aplicado").HasMaxLength(50);
                entity.Property(e => e.TipoBaseCalculo).HasColumnName("tipo_base_calculo");
                entity.Property(e => e.BaseImponible).HasColumnName("base_imponible").HasPrecision(18, 2);
                entity.Property(e => e.AlicuotaAplicada).HasColumnName("alicuota_aplicada").HasPrecision(9, 4);
                entity.Property(e => e.Importe).HasColumnName("importe").HasPrecision(18, 2);
                entity.Property(e => e.VigenciaDesdeAplicada).HasColumnName("vigencia_desde_aplicada");
                entity.Property(e => e.VigenciaHastaAplicada).HasColumnName("vigencia_hasta_aplicada");
                entity.Property(e => e.Resultado).HasColumnName("resultado");
                entity.Property(e => e.Motivo).HasColumnName("motivo").HasMaxLength(500);
                entity.Property(e => e.Activa).HasColumnName("activa");
                entity.Property(e => e.EsAutomatica).HasColumnName("es_automatica");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.VentaId)
                    .HasDatabaseName("ix_ventas_percepciones_aplicadas_venta");

                entity.HasIndex(e => e.RegimenPercepcionIibbId)
                    .HasDatabaseName("ix_ventas_percepciones_aplicadas_regimen");

                entity.HasIndex(e => new { e.VentaId, e.RegimenPercepcionIibbId, e.Activa })
                    .HasDatabaseName("ix_ventas_percepciones_aplicadas_venta_regimen_activa");

                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.PercepcionesIibb)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RegimenPercepcionIibb)
                    .WithMany()
                    .HasForeignKey(e => e.RegimenPercepcionIibbId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VentaDetalle>(entity =>
            {
                entity.ToTable("ventas_detalles", table =>
                {
                    table.HasCheckConstraint("ck_ventas_detalles_cantidad", "cantidad > 0");
                    table.HasCheckConstraint("ck_ventas_detalles_precio_unitario", "precio_unitario >= 0");
                    table.HasCheckConstraint("ck_ventas_detalles_descuento", "porcentaje_descuento >= 0 AND porcentaje_descuento <= 100");
                });
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.VentaId).HasColumnName("venta_id");
                entity.Property(e => e.NumeroLinea).HasColumnName("numero_linea");
                entity.Property(e => e.ItemFacturableId).HasColumnName("item_facturable_id");
                entity.Property(e => e.CodigoItem).HasColumnName("codigo_item").HasMaxLength(100);
                entity.Property(e => e.ItemFacturableDescripcion).HasColumnName("item_facturable_descripcion").HasMaxLength(200);
                entity.Property(e => e.CategoriaItemFacturableId).HasColumnName("categoria_item_facturable_id");
                entity.Property(e => e.CategoriaItemFacturableCodigo).HasColumnName("categoria_item_facturable_codigo").HasMaxLength(50);
                entity.Property(e => e.CategoriaItemFacturableDescripcion).HasColumnName("categoria_item_facturable_descripcion").HasMaxLength(200);
                entity.Property(e => e.UnidadMedidaVentaId).HasColumnName("unidad_medida_venta_id");
                entity.Property(e => e.UnidadMedidaCodigo).HasColumnName("unidad_medida_codigo").HasMaxLength(50);
                entity.Property(e => e.UnidadMedidaDescripcion).HasColumnName("unidad_medida_descripcion").HasMaxLength(200);
                entity.Property(e => e.UnidadMedidaAbreviatura).HasColumnName("unidad_medida_abreviatura").HasMaxLength(20);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsRequired();
                entity.Property(e => e.Cantidad).HasColumnName("cantidad").HasPrecision(18, 4);
                entity.Property(e => e.PrecioUnitario).HasColumnName("precio_unitario").HasPrecision(18, 4);
                entity.Property(e => e.PorcentajeDescuento).HasColumnName("porcentaje_descuento").HasPrecision(9, 4);
                entity.Property(e => e.ImporteBruto).HasColumnName("importe_bruto").HasPrecision(18, 2);
                entity.Property(e => e.ImporteDescuento).HasColumnName("importe_descuento").HasPrecision(18, 2);
                entity.Property(e => e.Neto).HasColumnName("neto").HasPrecision(18, 2);
                entity.Property(e => e.TratamientoIvaId).HasColumnName("tratamiento_iva_id");
                entity.Property(e => e.TratamientoIvaCodigo).HasColumnName("tratamiento_iva_codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.TratamientoIvaDescripcion).HasColumnName("tratamiento_iva_descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.TipoTratamientoIva).HasColumnName("tipo_tratamiento_iva");
                entity.Property(e => e.PorcentajeIvaAplicado).HasColumnName("porcentaje_iva_aplicado").HasPrecision(9, 4);
                entity.Property(e => e.ImporteIva).HasColumnName("importe_iva").HasPrecision(18, 2);
                entity.Property(e => e.NomencladorId).HasColumnName("nomenclador_id");
                entity.Property(e => e.NomencladorCodigo).HasColumnName("nomenclador_codigo").HasMaxLength(50);
                entity.Property(e => e.NomencladorDescripcion).HasColumnName("nomenclador_descripcion").HasMaxLength(250);
                entity.Property(e => e.TotalLinea).HasColumnName("total_linea").HasPrecision(18, 2);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.VentaId)
                    .HasDatabaseName("ix_ventas_detalles_venta_id");

                entity.HasIndex(e => new { e.VentaId, e.NumeroLinea })
                    .HasDatabaseName("ix_ventas_detalles_venta_linea")
                    .IsUnique();

                entity.HasIndex(e => e.TratamientoIvaId)
                    .HasDatabaseName("ix_ventas_detalles_tratamiento_iva_id");

                entity.HasIndex(e => e.NomencladorId)
                    .HasDatabaseName("ix_ventas_detalles_nomenclador_id");

                entity.HasIndex(e => e.ItemFacturableId)
                    .HasDatabaseName("ix_ventas_detalles_item_facturable_id");

                entity.HasIndex(e => new { e.VentaId, e.ItemFacturableId })
                    .HasDatabaseName("ix_ventas_detalles_venta_item_facturable_id");

                entity.HasIndex(e => e.CategoriaItemFacturableId)
                    .HasDatabaseName("ix_ventas_detalles_categoria_item_facturable_id");

                entity.HasIndex(e => e.UnidadMedidaVentaId)
                    .HasDatabaseName("ix_ventas_detalles_unidad_medida_venta_id");

                entity.HasOne(e => e.Venta)
                    .WithMany(v => v.Detalles)
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ItemFacturable)
                    .WithMany()
                    .HasForeignKey(e => e.ItemFacturableId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CategoriaItemFacturable)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaItemFacturableId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UnidadMedida)
                    .WithMany()
                    .HasForeignKey(e => e.UnidadMedidaVentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TratamientoIva)
                    .WithMany()
                    .HasForeignKey(e => e.TratamientoIvaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Nomenclador)
                    .WithMany()
                    .HasForeignKey(e => e.NomencladorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AlicuotaIvaVenta>(entity =>
            {
                entity.ToTable("ventas_alicuotas_iva");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.TipoTratamiento).HasColumnName("tipo_tratamiento").IsRequired();
                entity.Property(e => e.Porcentaje).HasColumnName("porcentaje").HasPrecision(9, 4);
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_alicuotas_iva_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_ventas_alicuotas_iva_activo");
            });

            modelBuilder.Entity<NomencladorFce>(entity =>
            {
                entity.ToTable("ventas_nomencladores_fce");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(250).IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_nomencladores_fce_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_ventas_nomencladores_fce_activo");
            });

            modelBuilder.Entity<PercepcionIibbEntreRios>(entity =>
            {
                entity.ToTable("ventas_percepciones_iibb");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(250).IsRequired();
                entity.Property(e => e.Jurisdiccion).HasColumnName("jurisdiccion").HasMaxLength(100).HasDefaultValue("Entre Rios").IsRequired();
                entity.Property(e => e.TipoTributo).HasColumnName("tipo_tributo").HasMaxLength(50).HasDefaultValue("PERCEPCION_IIBB").IsRequired();
                entity.Property(e => e.NumeroRegimen).HasColumnName("numero_regimen").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Porcentaje).HasColumnName("porcentaje").HasPrecision(9, 4);
                entity.Property(e => e.TipoBaseCalculo).HasColumnName("tipo_base_calculo").IsRequired();
                entity.Property(e => e.MontoMinimo).HasColumnName("monto_minimo").HasPrecision(18, 2);
                entity.Property(e => e.VigenciaDesde).HasColumnName("vigencia_desde");
                entity.Property(e => e.VigenciaHasta).HasColumnName("vigencia_hasta");
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_percepciones_iibb_codigo")
                    .IsUnique();

                entity.HasIndex(e => new { e.Jurisdiccion, e.TipoTributo, e.NumeroRegimen, e.TipoBaseCalculo, e.Activo })
                    .HasDatabaseName("ix_ventas_percepciones_iibb_equivalencia");

                entity.HasIndex(e => new { e.VigenciaDesde, e.VigenciaHasta })
                    .HasDatabaseName("ix_ventas_percepciones_iibb_vigencia");
            });

            modelBuilder.Entity<CategoriaItemFacturable>(entity =>
            {
                entity.ToTable("ventas_categorias_items_facturables", table =>
                {
                    table.HasCheckConstraint("ck_ventas_categorias_items_orden", "orden >= 0");
                });
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_categorias_items_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_ventas_categorias_items_activo");
            });

            modelBuilder.Entity<UnidadMedidaVenta>(entity =>
            {
                entity.ToTable("ventas_unidades_medida", table =>
                {
                    table.HasCheckConstraint("ck_ventas_unidades_medida_orden", "orden >= 0");
                });
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Abreviatura).HasColumnName("abreviatura").HasMaxLength(20);
                entity.Property(e => e.PermiteDecimales).HasColumnName("permite_decimales");
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_unidades_medida_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_ventas_unidades_medida_activo");
            });

            modelBuilder.Entity<ItemFacturable>(entity =>
            {
                entity.ToTable("ventas_items_facturables", table =>
                {
                    table.HasCheckConstraint("ck_ventas_items_facturables_precio", "precio_predeterminado IS NULL OR precio_predeterminado >= 0");
                    table.HasCheckConstraint("ck_ventas_items_facturables_orden", "orden >= 0");
                });
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.DescripcionAmpliada).HasColumnName("descripcion_ampliada").HasMaxLength(1000);
                entity.Property(e => e.CategoriaItemFacturableId).HasColumnName("categoria_item_facturable_id");
                entity.Property(e => e.UnidadMedidaVentaId).HasColumnName("unidad_medida_venta_id");
                entity.Property(e => e.TratamientoIvaPredeterminadoId).HasColumnName("tratamiento_iva_predeterminado_id");
                entity.Property(e => e.NomencladorPredeterminadoId).HasColumnName("nomenclador_predeterminado_id");
                entity.Property(e => e.PrecioPredeterminado).HasColumnName("precio_predeterminado").HasPrecision(18, 4);
                entity.Property(e => e.Activo).HasColumnName("activo");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_ventas_items_facturables_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Descripcion)
                    .HasDatabaseName("ix_ventas_items_facturables_descripcion");

                entity.HasIndex(e => e.CategoriaItemFacturableId)
                    .HasDatabaseName("ix_ventas_items_facturables_categoria_id");

                entity.HasIndex(e => e.UnidadMedidaVentaId)
                    .HasDatabaseName("ix_ventas_items_facturables_unidad_id");

                entity.HasIndex(e => e.TratamientoIvaPredeterminadoId)
                    .HasDatabaseName("ix_ventas_items_facturables_iva_id");

                entity.HasIndex(e => e.NomencladorPredeterminadoId)
                    .HasDatabaseName("ix_ventas_items_facturables_nomenclador_id");

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_ventas_items_facturables_activo");

                entity.HasOne(e => e.Categoria)
                    .WithMany(c => c.Items)
                    .HasForeignKey(e => e.CategoriaItemFacturableId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UnidadMedida)
                    .WithMany(u => u.Items)
                    .HasForeignKey(e => e.UnidadMedidaVentaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TratamientoIvaPredeterminado)
                    .WithMany()
                    .HasForeignKey(e => e.TratamientoIvaPredeterminadoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.NomencladorPredeterminado)
                    .WithMany()
                    .HasForeignKey(e => e.NomencladorPredeterminadoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Cobranza>(entity =>
            {
                entity.ToTable("cobranzas");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.ClienteExternoId).HasColumnName("cliente_externo_id").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("fecha");
                entity.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasMaxLength(10).IsRequired();
                entity.Property(e => e.Cotizacion).HasColumnName("cotizacion").HasPrecision(18, 6);
                entity.Property(e => e.ImporteTotal).HasColumnName("importe_total").HasPrecision(18, 2);
                entity.Property(e => e.Estado).HasColumnName("estado").HasDefaultValue(CobranzaEstado.Borrador);
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);
                entity.Property(e => e.FechaConfirmacion).HasColumnName("fecha_confirmacion");
                entity.Property(e => e.UsuarioConfirmacion).HasColumnName("usuario_confirmacion").HasMaxLength(100);
                entity.Property(e => e.AsientoContableId).HasColumnName("asiento_contable_id");

                entity.HasIndex(e => e.ClienteExternoId)
                    .HasDatabaseName("ix_cobranzas_cliente_externo_id");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("ix_cobranzas_estado");

                entity.HasIndex(e => e.Fecha)
                    .HasDatabaseName("ix_cobranzas_fecha");

                entity.HasIndex(e => e.AsientoContableId)
                    .HasDatabaseName("ix_cobranzas_asiento_contable_id");
            });

            modelBuilder.Entity<MedioPagoCobranza>(entity =>
            {
                entity.ToTable("cobranzas_medios_pago_catalogo");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
                entity.Property(e => e.CodigoConceptoContable).HasColumnName("codigo_concepto_contable").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.RequiereReferencia).HasColumnName("requiere_referencia").HasDefaultValue(false);
                entity.Property(e => e.RequiereBanco).HasColumnName("requiere_banco").HasDefaultValue(false);
                entity.Property(e => e.RequiereFechaValor).HasColumnName("requiere_fecha_valor").HasDefaultValue(false);
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_cobranzas_medios_pago_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_cobranzas_medios_pago_activo");

                entity.HasData(
                    BuildMedioPagoCobranzaSeed(1, "EFECTIVO", "Efectivo", "CAJA", false, false, false, 10),
                    BuildMedioPagoCobranzaSeed(2, "TRANSFERENCIA", "Transferencia bancaria", "BANCO", true, true, true, 20),
                    BuildMedioPagoCobranzaSeed(3, "CHEQUE", "Cheque de terceros", "CHEQUES_TERCEROS", true, true, true, 30),
                    BuildMedioPagoCobranzaSeed(4, "RETENCION_GANANCIAS", "Retencion de Ganancias sufrida", "RETENCION_GANANCIAS_SUFRIDA", true, false, false, 40),
                    BuildMedioPagoCobranzaSeed(5, "RETENCION_IIBB", "Retencion de IIBB sufrida", "RETENCION_IIBB_SUFRIDA", true, false, false, 50)
                );
            });

            modelBuilder.Entity<BancoCobranza>(entity =>
            {
                entity.ToTable("cobranzas_bancos_catalogo");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.Codigo)
                    .HasDatabaseName("ix_cobranzas_bancos_codigo")
                    .IsUnique();

                entity.HasIndex(e => e.Activo)
                    .HasDatabaseName("ix_cobranzas_bancos_activo");

                entity.HasData(
                    BuildBancoCobranzaSeed(1, "NACION", "Banco de la Nacion Argentina", 10),
                    BuildBancoCobranzaSeed(2, "PROVINCIA", "Banco Provincia", 20),
                    BuildBancoCobranzaSeed(3, "GALICIA", "Banco Galicia", 30),
                    BuildBancoCobranzaSeed(4, "SANTANDER", "Santander Rio", 40),
                    BuildBancoCobranzaSeed(5, "BBVA", "BBVA", 50),
                    BuildBancoCobranzaSeed(6, "MACRO", "Banco Macro", 60),
                    BuildBancoCobranzaSeed(7, "CREDICOOP", "Banco Credicoop", 70),
                    BuildBancoCobranzaSeed(8, "ICBC", "ICBC", 80),
                    BuildBancoCobranzaSeed(9, "CIUDAD", "Banco Ciudad", 90),
                    BuildBancoCobranzaSeed(10, "PATAGONIA", "Banco Patagonia", 100),
                    BuildBancoCobranzaSeed(11, "SUPERVIELLE", "Banco Supervielle", 110),
                    BuildBancoCobranzaSeed(12, "COMAFI", "Banco Comafi", 120),
                    BuildBancoCobranzaSeed(13, "HSBC", "HSBC", 130),
                    BuildBancoCobranzaSeed(14, "OTRO", "Otro banco", 999)
                );
            });

            modelBuilder.Entity<CobranzaMedioPago>(entity =>
            {
                entity.ToTable("cobranzas_medios_pago");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.CobranzaId).HasColumnName("cobranza_id");
                entity.Property(e => e.MedioPagoCobranzaId).HasColumnName("medio_pago_cobranza_id");
                entity.Property(e => e.BancoCobranzaId).HasColumnName("banco_cobranza_id");
                entity.Property(e => e.Importe).HasColumnName("importe").HasPrecision(18, 2);
                entity.Property(e => e.Banco).HasColumnName("banco").HasMaxLength(200);
                entity.Property(e => e.NumeroReferencia).HasColumnName("numero_referencia").HasMaxLength(100);
                entity.Property(e => e.FechaValor).HasColumnName("fecha_valor");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(1000);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.CobranzaId)
                    .HasDatabaseName("ix_cobranzas_medios_pago_cobranza_id");

                entity.HasIndex(e => e.MedioPagoCobranzaId)
                    .HasDatabaseName("ix_cobranzas_medios_pago_medio_id");

                entity.HasIndex(e => e.BancoCobranzaId)
                    .HasDatabaseName("ix_cobranzas_medios_pago_banco_id");

                entity.HasOne(e => e.Cobranza)
                    .WithMany(c => c.MediosPago)
                    .HasForeignKey(e => e.CobranzaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.MedioPago)
                    .WithMany(m => m.CobranzasMediosPago)
                    .HasForeignKey(e => e.MedioPagoCobranzaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BancoCatalogo)
                    .WithMany(b => b.CobranzasMediosPago)
                    .HasForeignKey(e => e.BancoCobranzaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CobranzaAplicacionFactura>(entity =>
            {
                entity.ToTable("cobranzas_aplicaciones_facturas");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.CobranzaId).HasColumnName("cobranza_id");
                entity.Property(e => e.VentaId).HasColumnName("venta_id");
                entity.Property(e => e.ImporteAplicado).HasColumnName("importe_aplicado").HasPrecision(18, 2);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();
                entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");
                entity.Property(e => e.UsuarioModificacion).HasColumnName("usuario_modificacion").HasMaxLength(100);

                entity.HasIndex(e => e.CobranzaId)
                    .HasDatabaseName("ix_cobranzas_aplicaciones_facturas_cobranza_id");

                entity.HasIndex(e => e.VentaId)
                    .HasDatabaseName("ix_cobranzas_aplicaciones_facturas_venta_id");

                entity.HasIndex(e => new { e.CobranzaId, e.VentaId })
                    .HasDatabaseName("ix_cobranzas_aplicaciones_facturas_cobranza_venta")
                    .IsUnique();

                entity.HasOne(e => e.Cobranza)
                    .WithMany(c => c.AplicacionesFactura)
                    .HasForeignKey(e => e.CobranzaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Venta)
                    .WithMany()
                    .HasForeignKey(e => e.VentaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CobranzaAplicacionObligacion>(entity =>
            {
                entity.ToTable("cobranzas_aplicaciones_obligaciones");
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.CobranzaAplicacionFacturaId).HasColumnName("cobranza_aplicacion_factura_id");
                entity.Property(e => e.CuotaComercialId).HasColumnName("cuota_comercial_id");
                entity.Property(e => e.TipoObligacion).HasColumnName("tipo_obligacion").HasMaxLength(50).IsRequired();
                entity.Property(e => e.ImporteAplicado).HasColumnName("importe_aplicado").HasPrecision(18, 2);
                entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
                entity.Property(e => e.UsuarioAlta).HasColumnName("usuario_alta").HasMaxLength(100).IsRequired();

                entity.HasIndex(e => e.CobranzaAplicacionFacturaId)
                    .HasDatabaseName("ix_cobranzas_aplicaciones_obligaciones_aplicacion_id");

                entity.HasIndex(e => e.CuotaComercialId)
                    .HasDatabaseName("ix_cobranzas_aplicaciones_obligaciones_cuota_id");

                entity.HasOne(e => e.AplicacionFactura)
                    .WithMany(a => a.AplicacionesObligacion)
                    .HasForeignKey(e => e.CobranzaAplicacionFacturaId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CuotaComercial)
                    .WithMany()
                    .HasForeignKey(e => e.CuotaComercialId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static TipoComprobanteVenta BuildTipoComprobanteSeed(
            int id,
            string codigo,
            string descripcion,
            string? letra,
            string tipoFiscal,
            bool esCreditoElectronica,
            bool esExportacion,
            bool requiereNomenclador,
            bool permiteIva,
            int signo,
            bool activo,
            int orden)
        {
            return new TipoComprobanteVenta
            {
                Id = id,
                Codigo = codigo,
                Descripcion = descripcion,
                Letra = letra,
                TipoFiscal = tipoFiscal,
                EsCreditoElectronica = esCreditoElectronica,
                EsExportacion = esExportacion,
                RequiereNomenclador = requiereNomenclador,
                PermiteIva = permiteIva,
                Signo = signo,
                Activo = activo,
                Orden = orden,
                FechaAlta = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
                UsuarioAlta = "Sistema"
            };
        }

        private static MedioPagoCobranza BuildMedioPagoCobranzaSeed(
            int id,
            string codigo,
            string descripcion,
            string codigoConceptoContable,
            bool requiereReferencia,
            bool requiereBanco,
            bool requiereFechaValor,
            int orden)
        {
            return new MedioPagoCobranza
            {
                Id = id,
                Codigo = codigo,
                Descripcion = descripcion,
                CodigoConceptoContable = codigoConceptoContable,
                Activo = true,
                RequiereReferencia = requiereReferencia,
                RequiereBanco = requiereBanco,
                RequiereFechaValor = requiereFechaValor,
                Orden = orden,
                FechaAlta = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
                UsuarioAlta = "Sistema"
            };
        }

        private static BancoCobranza BuildBancoCobranzaSeed(int id, string codigo, string nombre, int orden)
        {
            return new BancoCobranza
            {
                Id = id,
                Codigo = codigo,
                Nombre = nombre,
                Activo = true,
                Orden = orden,
                FechaAlta = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                UsuarioAlta = "Sistema"
            };
        }
    }
}

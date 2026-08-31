using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CobranzasVia1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobranzas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "character varying(10)", maxLength: 10, nullable: false),
                    cotizacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    importetotal = table.Column<decimal>(name: "importe_total", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true),
                    fechaconfirmacion = table.Column<DateTime>(name: "fecha_confirmacion", type: "timestamp with time zone", nullable: true),
                    usuarioconfirmacion = table.Column<string>(name: "usuario_confirmacion", type: "character varying(100)", maxLength: 100, nullable: true),
                    asientocontableid = table.Column<int>(name: "asiento_contable_id", type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cobranzas_medios_pago_catalogo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    codigoconceptocontable = table.Column<string>(name: "codigo_concepto_contable", type: "character varying(100)", maxLength: 100, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    requierereferencia = table.Column<bool>(name: "requiere_referencia", type: "boolean", nullable: false, defaultValue: false),
                    requierebanco = table.Column<bool>(name: "requiere_banco", type: "boolean", nullable: false, defaultValue: false),
                    requierefechavalor = table.Column<bool>(name: "requiere_fecha_valor", type: "boolean", nullable: false, defaultValue: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_medios_pago_catalogo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cobranzas_aplicaciones_facturas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranzaid = table.Column<int>(name: "cobranza_id", type: "integer", nullable: false),
                    ventaid = table.Column<int>(name: "venta_id", type: "integer", nullable: false),
                    importeaplicado = table.Column<decimal>(name: "importe_aplicado", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_aplicaciones_facturas", x => x.id);
                    table.ForeignKey(
                        name: "FK_cobranzas_aplicaciones_facturas_cobranzas_cobranza_id",
                        column: x => x.cobranzaid,
                        principalTable: "cobranzas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cobranzas_aplicaciones_facturas_ventas_venta_id",
                        column: x => x.ventaid,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobranzas_medios_pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranzaid = table.Column<int>(name: "cobranza_id", type: "integer", nullable: false),
                    mediopagocobranzaid = table.Column<int>(name: "medio_pago_cobranza_id", type: "integer", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    banco = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numeroreferencia = table.Column<string>(name: "numero_referencia", type: "character varying(100)", maxLength: 100, nullable: true),
                    fechavalor = table.Column<DateTime>(name: "fecha_valor", type: "timestamp with time zone", nullable: true),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_medios_pago", x => x.id);
                    table.ForeignKey(
                        name: "FK_cobranzas_medios_pago_cobranzas_cobranza_id",
                        column: x => x.cobranzaid,
                        principalTable: "cobranzas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cobranzas_medios_pago_cobranzas_medios_pago_catalogo_medio_~",
                        column: x => x.mediopagocobranzaid,
                        principalTable: "cobranzas_medios_pago_catalogo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cobranzas_aplicaciones_obligaciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranzaaplicacionfacturaid = table.Column<int>(name: "cobranza_aplicacion_factura_id", type: "integer", nullable: false),
                    cuotacomercialid = table.Column<int>(name: "cuota_comercial_id", type: "integer", nullable: false),
                    tipoobligacion = table.Column<string>(name: "tipo_obligacion", type: "character varying(50)", maxLength: 50, nullable: false),
                    importeaplicado = table.Column<decimal>(name: "importe_aplicado", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_aplicaciones_obligaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_cobranzas_aplicaciones_obligaciones_cobranzas_aplicaciones_~",
                        column: x => x.cobranzaaplicacionfacturaid,
                        principalTable: "cobranzas_aplicaciones_facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cobranzas_aplicaciones_obligaciones_cuotas_comerciales_cuot~",
                        column: x => x.cuotacomercialid,
                        principalTable: "cuotas_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "cobranzas_medios_pago_catalogo",
                columns: new[] { "id", "activo", "codigo", "codigo_concepto_contable", "descripcion", "fecha_alta", "orden", "usuario_alta" },
                values: new object[] { 1, true, "EFECTIVO", "CAJA", "Efectivo", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 10, "Sistema" });

            migrationBuilder.InsertData(
                table: "cobranzas_medios_pago_catalogo",
                columns: new[] { "id", "activo", "codigo", "codigo_concepto_contable", "descripcion", "fecha_alta", "orden", "requiere_banco", "requiere_fecha_valor", "requiere_referencia", "usuario_alta" },
                values: new object[,]
                {
                    { 2, true, "TRANSFERENCIA", "BANCO", "Transferencia bancaria", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 20, true, true, true, "Sistema" },
                    { 3, true, "CHEQUE", "CHEQUES_TERCEROS", "Cheque de terceros", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 30, true, true, true, "Sistema" }
                });

            migrationBuilder.InsertData(
                table: "cobranzas_medios_pago_catalogo",
                columns: new[] { "id", "activo", "codigo", "codigo_concepto_contable", "descripcion", "fecha_alta", "orden", "requiere_referencia", "usuario_alta" },
                values: new object[,]
                {
                    { 4, true, "RETENCION_GANANCIAS", "RETENCION_GANANCIAS_SUFRIDA", "Retencion de Ganancias sufrida", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 40, true, "Sistema" },
                    { 5, true, "RETENCION_IIBB", "RETENCION_IIBB_SUFRIDA", "Retencion de IIBB sufrida", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 50, true, "Sistema" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_asiento_contable_id",
                table: "cobranzas",
                column: "asiento_contable_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_cliente_externo_id",
                table: "cobranzas",
                column: "cliente_externo_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_estado",
                table: "cobranzas",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_fecha",
                table: "cobranzas",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_aplicaciones_facturas_cobranza_id",
                table: "cobranzas_aplicaciones_facturas",
                column: "cobranza_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_aplicaciones_facturas_cobranza_venta",
                table: "cobranzas_aplicaciones_facturas",
                columns: new[] { "cobranza_id", "venta_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_aplicaciones_facturas_venta_id",
                table: "cobranzas_aplicaciones_facturas",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_aplicaciones_obligaciones_aplicacion_id",
                table: "cobranzas_aplicaciones_obligaciones",
                column: "cobranza_aplicacion_factura_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_aplicaciones_obligaciones_cuota_id",
                table: "cobranzas_aplicaciones_obligaciones",
                column: "cuota_comercial_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_cobranza_id",
                table: "cobranzas_medios_pago",
                column: "cobranza_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_medio_id",
                table: "cobranzas_medios_pago",
                column: "medio_pago_cobranza_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_activo",
                table: "cobranzas_medios_pago_catalogo",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_codigo",
                table: "cobranzas_medios_pago_catalogo",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cobranzas_aplicaciones_obligaciones");

            migrationBuilder.DropTable(
                name: "cobranzas_medios_pago");

            migrationBuilder.DropTable(
                name: "cobranzas_aplicaciones_facturas");

            migrationBuilder.DropTable(
                name: "cobranzas_medios_pago_catalogo");

            migrationBuilder.DropTable(
                name: "cobranzas");
        }
    }
}

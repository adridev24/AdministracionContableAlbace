using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesDocumentConfigAndPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "punto_venta_comprobante_id",
                table: "ventas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "es_credito_electronica",
                table: "tipos_comprobante_venta",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "es_exportacion",
                table: "tipos_comprobante_venta",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_alta",
                table: "tipos_comprobante_venta",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_modificacion",
                table: "tipos_comprobante_venta",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "permite_iva",
                table: "tipos_comprobante_venta",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "requiere_nomenclador",
                table: "tipos_comprobante_venta",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tipo_fiscal",
                table: "tipos_comprobante_venta",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Local");

            migrationBuilder.AddColumn<string>(
                name: "usuario_alta",
                table: "tipos_comprobante_venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Sistema");

            migrationBuilder.AddColumn<string>(
                name: "usuario_modificacion",
                table: "tipos_comprobante_venta",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "puntos_venta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puntos_venta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "puntos_venta_comprobantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    puntoventaid = table.Column<int>(name: "punto_venta_id", type: "integer", nullable: false),
                    tipocomprobanteventaid = table.Column<int>(name: "tipo_comprobante_venta_id", type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puntos_venta_comprobantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_puntos_venta_comprobantes_puntos_venta_punto_venta_id",
                        column: x => x.puntoventaid,
                        principalTable: "puntos_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_puntos_venta_comprobantes_tipos_comprobante_venta_tipo_comp~",
                        column: x => x.tipocomprobanteventaid,
                        principalTable: "tipos_comprobante_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "fecha_alta", "fecha_modificacion", "permite_iva", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Local", "Sistema", null });

            migrationBuilder.UpdateData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "fecha_alta", "fecha_modificacion", "permite_iva", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Local", "Sistema", null });

            migrationBuilder.UpdateData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "fecha_alta", "fecha_modificacion", "permite_iva", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Local", "Sistema", null });

            migrationBuilder.UpdateData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "fecha_alta", "fecha_modificacion", "permite_iva", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Local", "Sistema", null });

            migrationBuilder.UpdateData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "fecha_alta", "fecha_modificacion", "permite_iva", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Local", "Sistema", null });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_exportacion", "fecha_alta", "fecha_modificacion", "letra", "orden", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { 6, true, "FACTURA_E", "Factura E", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "E", 60, 1, "Exportacion", "Sistema", null });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_credito_electronica", "fecha_alta", "fecha_modificacion", "letra", "orden", "permite_iva", "requiere_nomenclador", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { 7, true, "FCE_MIPYME_A_CON_NOMENCLADOR", "Factura de Credito Electronica MiPyME A con nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 70, true, true, 1, "Local", "Sistema", null });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_credito_electronica", "fecha_alta", "fecha_modificacion", "letra", "orden", "permite_iva", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[] { 8, true, "FCE_MIPYME_A_SIN_NOMENCLADOR", "Factura de Credito Electronica MiPyME A sin nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 80, true, 1, "Local", "Sistema", null });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "fecha_alta", "fecha_modificacion", "letra", "orden", "permite_iva", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[,]
                {
                    { 9, true, "NOTA_DEBITO_A", "Nota de debito A", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 90, true, 1, "Local", "Sistema", null },
                    { 10, true, "NOTA_CREDITO_A", "Nota de credito A", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 100, true, -1, "Local", "Sistema", null },
                    { 11, true, "NOTA_DEBITO_B", "Nota de debito B", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "B", 110, true, 1, "Local", "Sistema", null },
                    { 12, true, "NOTA_CREDITO_B", "Nota de credito B", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "B", 120, true, -1, "Local", "Sistema", null }
                });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_exportacion", "fecha_alta", "fecha_modificacion", "letra", "orden", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[,]
                {
                    { 13, true, "NOTA_DEBITO_E", "Nota de debito E", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "E", 130, 1, "Exportacion", "Sistema", null },
                    { 14, true, "NOTA_CREDITO_E", "Nota de credito E", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "E", 140, -1, "Exportacion", "Sistema", null }
                });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_credito_electronica", "fecha_alta", "fecha_modificacion", "letra", "orden", "permite_iva", "requiere_nomenclador", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[,]
                {
                    { 15, true, "FCE_MIPYME_NOTA_DEBITO_A_CON_NOMENCLADOR", "Nota de debito FCE MiPyME A con nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 150, true, true, 1, "Local", "Sistema", null },
                    { 16, true, "FCE_MIPYME_NOTA_CREDITO_A_CON_NOMENCLADOR", "Nota de credito FCE MiPyME A con nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 160, true, true, -1, "Local", "Sistema", null }
                });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "es_credito_electronica", "fecha_alta", "fecha_modificacion", "letra", "orden", "permite_iva", "signo", "tipo_fiscal", "usuario_alta", "usuario_modificacion" },
                values: new object[,]
                {
                    { 17, true, "FCE_MIPYME_NOTA_DEBITO_A_SIN_NOMENCLADOR", "Nota de debito FCE MiPyME A sin nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 170, true, 1, "Local", "Sistema", null },
                    { 18, true, "FCE_MIPYME_NOTA_CREDITO_A_SIN_NOMENCLADOR", "Nota de credito FCE MiPyME A sin nomenclador", true, new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "A", 180, true, -1, "Local", "Sistema", null }
                });

            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('tipos_comprobante_venta', 'id'),
                    GREATEST(
                        (SELECT MAX(id) FROM tipos_comprobante_venta) + 1,
                        nextval(pg_get_serial_sequence('tipos_comprobante_venta', 'id'))),
                    false);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_punto_venta_comprobante_id",
                table: "ventas",
                column: "punto_venta_comprobante_id");

            migrationBuilder.CreateIndex(
                name: "ix_puntos_venta_numero",
                table: "puntos_venta",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_puntos_venta_comprobantes_punto_tipo",
                table: "puntos_venta_comprobantes",
                columns: new[] { "punto_venta_id", "tipo_comprobante_venta_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_comprobantes_tipo_comprobante_venta_id",
                table: "puntos_venta_comprobantes",
                column: "tipo_comprobante_venta_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_puntos_venta_comprobantes_punto_venta_comprobante_id",
                table: "ventas",
                column: "punto_venta_comprobante_id",
                principalTable: "puntos_venta_comprobantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_puntos_venta_comprobantes_punto_venta_comprobante_id",
                table: "ventas");

            migrationBuilder.DropTable(
                name: "puntos_venta_comprobantes");

            migrationBuilder.DropTable(
                name: "puntos_venta");

            migrationBuilder.DropIndex(
                name: "ix_ventas_punto_venta_comprobante_id",
                table: "ventas");

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "tipos_comprobante_venta",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DropColumn(
                name: "punto_venta_comprobante_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "es_credito_electronica",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "es_exportacion",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "fecha_alta",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "fecha_modificacion",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "permite_iva",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "requiere_nomenclador",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "tipo_fiscal",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "usuario_alta",
                table: "tipos_comprobante_venta");

            migrationBuilder.DropColumn(
                name: "usuario_modificacion",
                table: "tipos_comprobante_venta");
        }
    }
}

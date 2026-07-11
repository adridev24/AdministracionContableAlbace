using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSalesHeaderModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tipos_comprobante_venta",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    letra = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    signo = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_comprobante_venta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tipocomprobanteventaid = table.Column<int>(name: "tipo_comprobante_venta_id", type: "integer", nullable: false),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    obraexternaid = table.Column<string>(name: "obra_externa_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    fechacomprobante = table.Column<DateTime>(name: "fecha_comprobante", type: "timestamp with time zone", nullable: false),
                    puntoventa = table.Column<int>(name: "punto_venta", type: "integer", nullable: false),
                    numerocomprobante = table.Column<long>(name: "numero_comprobante", type: "bigint", nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "character varying(10)", maxLength: 10, nullable: false),
                    cotizacion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_tipos_comprobante_venta_tipo_comprobante_venta_id",
                        column: x => x.tipocomprobanteventaid,
                        principalTable: "tipos_comprobante_venta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "tipos_comprobante_venta",
                columns: new[] { "id", "activo", "codigo", "descripcion", "letra", "orden", "signo" },
                values: new object[,]
                {
                    { 1, true, "FACTURA_A", "Factura A", "A", 10, 1 },
                    { 2, true, "FACTURA_B", "Factura B", "B", 20, 1 },
                    { 3, true, "FACTURA_C", "Factura C", "C", 30, 1 },
                    { 4, true, "NOTA_DEBITO", "Nota de debito", null, 40, 1 },
                    { 5, true, "NOTA_CREDITO", "Nota de credito", null, 50, -1 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_tipos_comprobante_venta_codigo",
                table: "tipos_comprobante_venta",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_cliente_externo_id",
                table: "ventas",
                column: "cliente_externo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_estado",
                table: "ventas",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_fecha_comprobante",
                table: "ventas",
                column: "fecha_comprobante");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_numeracion",
                table: "ventas",
                columns: new[] { "tipo_comprobante_venta_id", "punto_venta", "numero_comprobante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_obra_externa_id",
                table: "ventas",
                column: "obra_externa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropTable(
                name: "tipos_comprobante_venta");
        }
    }
}

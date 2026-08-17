using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesInvoiceConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "asiento_contable_id",
                table: "ventas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_confirmacion",
                table: "ventas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_confirmacion",
                table: "ventas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ventas_movimientos_cuenta_corriente",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    obraexternaid = table.Column<string>(name: "obra_externa_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipomovimiento = table.Column<string>(name: "tipo_movimiento", type: "character varying(50)", maxLength: 50, nullable: false),
                    debe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    haber = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moduloorigen = table.Column<string>(name: "modulo_origen", type: "character varying(50)", maxLength: 50, nullable: false),
                    idorigen = table.Column<string>(name: "id_origen", type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_movimientos_cuenta_corriente", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_asiento_contable_id",
                table: "ventas",
                column: "asiento_contable_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_mov_cc_cliente",
                table: "ventas_movimientos_cuenta_corriente",
                column: "cliente_externo_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_mov_cc_obra",
                table: "ventas_movimientos_cuenta_corriente",
                column: "obra_externa_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_mov_cc_origen_tipo",
                table: "ventas_movimientos_cuenta_corriente",
                columns: new[] { "modulo_origen", "id_origen", "tipo_movimiento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_movimientos_cuenta_corriente");

            migrationBuilder.DropIndex(
                name: "ix_ventas_asiento_contable_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "asiento_contable_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "fecha_confirmacion",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "usuario_confirmacion",
                table: "ventas");
        }
    }
}

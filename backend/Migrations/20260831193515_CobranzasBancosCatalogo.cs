using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CobranzasBancosCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "banco_cobranza_id",
                table: "cobranzas_medios_pago",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cobranzas_bancos_catalogo",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_bancos_catalogo", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "cobranzas_bancos_catalogo",
                columns: new[] { "id", "activo", "codigo", "fecha_alta", "nombre", "orden", "usuario_alta" },
                values: new object[,]
                {
                    { 1, true, "NACION", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco de la Nacion Argentina", 10, "Sistema" },
                    { 2, true, "PROVINCIA", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Provincia", 20, "Sistema" },
                    { 3, true, "GALICIA", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Galicia", 30, "Sistema" },
                    { 4, true, "SANTANDER", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Santander Rio", 40, "Sistema" },
                    { 5, true, "BBVA", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "BBVA", 50, "Sistema" },
                    { 6, true, "MACRO", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Macro", 60, "Sistema" },
                    { 7, true, "CREDICOOP", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Credicoop", 70, "Sistema" },
                    { 8, true, "ICBC", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "ICBC", 80, "Sistema" },
                    { 9, true, "CIUDAD", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Ciudad", 90, "Sistema" },
                    { 10, true, "PATAGONIA", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Patagonia", 100, "Sistema" },
                    { 11, true, "SUPERVIELLE", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Supervielle", 110, "Sistema" },
                    { 12, true, "COMAFI", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Banco Comafi", 120, "Sistema" },
                    { 13, true, "HSBC", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "HSBC", 130, "Sistema" },
                    { 14, true, "OTRO", new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Utc), "Otro banco", 999, "Sistema" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_banco_id",
                table: "cobranzas_medios_pago",
                column: "banco_cobranza_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_bancos_activo",
                table: "cobranzas_bancos_catalogo",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_bancos_codigo",
                table: "cobranzas_bancos_catalogo",
                column: "codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cobranzas_medios_pago_cobranzas_bancos_catalogo_banco_cobra~",
                table: "cobranzas_medios_pago",
                column: "banco_cobranza_id",
                principalTable: "cobranzas_bancos_catalogo",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cobranzas_medios_pago_cobranzas_bancos_catalogo_banco_cobra~",
                table: "cobranzas_medios_pago");

            migrationBuilder.DropTable(
                name: "cobranzas_bancos_catalogo");

            migrationBuilder.DropIndex(
                name: "ix_cobranzas_medios_pago_banco_id",
                table: "cobranzas_medios_pago");

            migrationBuilder.DropColumn(
                name: "banco_cobranza_id",
                table: "cobranzas_medios_pago");
        }
    }
}

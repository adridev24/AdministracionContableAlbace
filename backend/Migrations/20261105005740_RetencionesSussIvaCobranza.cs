using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class RetencionesSussIvaCobranza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "cobranzas_medios_pago_catalogo",
                columns: new[] { "id", "activo", "codigo", "codigo_concepto_contable", "descripcion", "fecha_alta", "orden", "requiere_referencia", "usuario_alta" },
                values: new object[,]
                {
                    { 6, true, "RETENCION_SUSS", "RETENCION_SUSS_SUFRIDA", "Retención SUSS sufrida", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 60, true, "Sistema" },
                    { 7, true, "RETENCION_IVA", "RETENCION_IVA_SUFRIDA", "Retención IVA sufrida", new DateTime(2026, 8, 17, 0, 0, 0, 0, DateTimeKind.Utc), 70, true, "Sistema" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "cobranzas_medios_pago_catalogo",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "cobranzas_medios_pago_catalogo",
                keyColumn: "id",
                keyValue: 7);
        }
    }
}

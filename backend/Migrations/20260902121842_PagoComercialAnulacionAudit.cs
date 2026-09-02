using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class PagoComercialAnulacionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_anulacion",
                table: "pagos_comerciales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                table: "pagos_comerciales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_anulacion",
                table: "pagos_comerciales",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_anulacion",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "usuario_anulacion",
                table: "pagos_comerciales");
        }
    }
}

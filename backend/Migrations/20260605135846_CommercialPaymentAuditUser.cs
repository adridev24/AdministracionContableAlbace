using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CommercialPaymentAuditUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_alta",
                table: "pagos_comerciales",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "usuario_alta",
                table: "pagos_comerciales",
                type: "text",
                nullable: false,
                defaultValue: "migracion");

            migrationBuilder.AddColumn<string>(
                name: "usuario_aplicacion",
                table: "aplicaciones_pago_comerciales",
                type: "text",
                nullable: false,
                defaultValue: "migracion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_alta",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "usuario_alta",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "usuario_aplicacion",
                table: "aplicaciones_pago_comerciales");
        }
    }
}

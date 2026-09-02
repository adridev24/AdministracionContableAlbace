using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CobranzaAnulacionIntegral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.DateTime>(
                name: "fecha_anulacion",
                table: "cobranzas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_anulacion",
                table: "cobranzas",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_anulacion",
                table: "cobranzas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_anulacion",
                table: "cobranzas");

            migrationBuilder.DropColumn(
                name: "motivo_anulacion",
                table: "cobranzas");

            migrationBuilder.DropColumn(
                name: "usuario_anulacion",
                table: "cobranzas");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class AccountingOptionalConcepts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "es_obligatorio",
                table: "configuraciones_contables_detalle",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("UPDATE configuraciones_contables_detalle SET es_obligatorio = TRUE WHERE es_obligatorio IS NULL;");

            migrationBuilder.AlterColumn<bool>(
                name: "es_obligatorio",
                table: "configuraciones_contables_detalle",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_obligatorio",
                table: "configuraciones_contables_detalle");
        }
    }
}

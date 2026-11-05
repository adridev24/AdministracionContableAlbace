using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class FacturacionSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<string>(
                name: "FiscalSimpleJson",
                table: "ventas",
                type: "text",
                nullable: true);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "FiscalSimpleJson",
                table: "ventas");


        }
    }
}

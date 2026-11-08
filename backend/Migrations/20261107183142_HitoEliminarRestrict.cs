using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class HitoEliminarRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.AddForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales",
                column: "hito_comercial_via_id",
                principalTable: "hitos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.AddForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales",
                column: "hito_comercial_via_id",
                principalTable: "hitos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

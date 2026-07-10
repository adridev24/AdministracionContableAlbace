using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixCommercialPaymentEnumDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE pagos_comerciales SET tipo_imputacion = 4 WHERE origen_pago NOT IN (0, 1) AND tipo_imputacion = 0;");
            migrationBuilder.Sql("UPDATE pagos_comerciales SET origen_pago = 0 WHERE origen_pago NOT IN (0, 1);");

            migrationBuilder.AlterColumn<int>(
                name: "tipo_imputacion",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "origen_pago",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "tipo_imputacion",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 4,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "modalidad_cobro",
                table: "acuerdos_comerciales_vias",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "tipo_imputacion",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 3);

            migrationBuilder.AlterColumn<int>(
                name: "origen_pago",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "tipo_imputacion",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 4);

            migrationBuilder.AlterColumn<int>(
                name: "modalidad_cobro",
                table: "acuerdos_comerciales_vias",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}

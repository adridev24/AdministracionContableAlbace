using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CommercialModalidadHitosPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "origen_pago",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "tipo_imputacion",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AlterColumn<int>(
                name: "cuota_comercial_id",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "hito_comercial_via_id",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observaciones",
                table: "aplicaciones_pago_comerciales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_imputacion",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "modalidad_cobro",
                table: "acuerdos_comerciales_vias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE acuerdos_comerciales_vias SET modalidad_cobro = 1 WHERE via_operacion = 1;");

            migrationBuilder.CreateTable(
                name: "hitos_comerciales_vias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acuerdocomercialviaid = table.Column<int>(name: "acuerdo_comercial_via_id", type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    importeestimado = table.Column<decimal>(name: "importe_estimado", type: "numeric", nullable: false),
                    fechareferencia = table.Column<DateTime>(name: "fecha_referencia", type: "timestamp with time zone", nullable: false),
                    importeaplicado = table.Column<decimal>(name: "importe_aplicado", type: "numeric", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hitos_comerciales_vias", x => x.id);
                    table.ForeignKey(
                        name: "FK_hitos_comerciales_vias_acuerdos_comerciales_vias_acuerdo_co~",
                        column: x => x.acuerdocomercialviaid,
                        principalTable: "acuerdos_comerciales_vias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aplicaciones_pago_comerciales_hito_comercial_via_id",
                table: "aplicaciones_pago_comerciales",
                column: "hito_comercial_via_id");

            migrationBuilder.CreateIndex(
                name: "IX_hitos_comerciales_vias_acuerdo_comercial_via_id",
                table: "hitos_comerciales_vias",
                column: "acuerdo_comercial_via_id");

            migrationBuilder.AddForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales",
                column: "hito_comercial_via_id",
                principalTable: "hitos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_aplicaciones_pago_comerciales_hitos_comerciales_vias_hito_c~",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropTable(
                name: "hitos_comerciales_vias");

            migrationBuilder.DropIndex(
                name: "IX_aplicaciones_pago_comerciales_hito_comercial_via_id",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropColumn(
                name: "origen_pago",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "tipo_imputacion",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "hito_comercial_via_id",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropColumn(
                name: "observaciones",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropColumn(
                name: "tipo_imputacion",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropColumn(
                name: "modalidad_cobro",
                table: "acuerdos_comerciales_vias");

            migrationBuilder.AlterColumn<int>(
                name: "cuota_comercial_id",
                table: "aplicaciones_pago_comerciales",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class AjusteCuotaComercial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ajustes_cuotas_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuotacomercialid = table.Column<int>(name: "cuota_comercial_id", type: "integer", nullable: false),
                    planpagoid = table.Column<int>(name: "plan_pago_id", type: "integer", nullable: false),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    tipoajuste = table.Column<int>(name: "tipo_ajuste", type: "integer", nullable: false),
                    importeanterior = table.Column<decimal>(name: "importe_anterior", type: "numeric", nullable: true),
                    importenuevo = table.Column<decimal>(name: "importe_nuevo", type: "numeric", nullable: true),
                    fechavencimientoanterior = table.Column<DateTime>(name: "fecha_vencimiento_anterior", type: "timestamp with time zone", nullable: true),
                    fechavencimientonueva = table.Column<DateTime>(name: "fecha_vencimiento_nueva", type: "timestamp with time zone", nullable: true),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    fechaajuste = table.Column<DateTime>(name: "fecha_ajuste", type: "timestamp with time zone", nullable: false),
                    usuarioajuste = table.Column<string>(name: "usuario_ajuste", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_cuotas_comerciales", x => x.id);
                    table.ForeignKey(
                        name: "FK_ajustes_cuotas_comerciales_acuerdos_comerciales_acuerdo_com~",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ajustes_cuotas_comerciales_cuotas_comerciales_cuota_comerci~",
                        column: x => x.cuotacomercialid,
                        principalTable: "cuotas_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ajustes_cuotas_comerciales_planes_pago_plan_pago_id",
                        column: x => x.planpagoid,
                        principalTable: "planes_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_cuotas_comerciales_acuerdo_comercial_id",
                table: "ajustes_cuotas_comerciales",
                column: "acuerdo_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_cuotas_comerciales_cuota_comercial_id",
                table: "ajustes_cuotas_comerciales",
                column: "cuota_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_cuotas_comerciales_plan_pago_id",
                table: "ajustes_cuotas_comerciales",
                column: "plan_pago_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ajustes_cuotas_comerciales");
        }
    }
}

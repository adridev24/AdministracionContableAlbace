using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CommercialViaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_planes_pago_acuerdos_comerciales_acuerdo_comercial_id",
                table: "planes_pago");

            migrationBuilder.DropIndex(
                name: "IX_planes_pago_acuerdo_comercial_id",
                table: "planes_pago");

            migrationBuilder.AlterColumn<int>(
                name: "acuerdo_comercial_id",
                table: "planes_pago",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "acuerdos_comerciales_vias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    viaoperacion = table.Column<int>(name: "via_operacion", type: "integer", nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "text", nullable: false),
                    montooriginal = table.Column<decimal>(name: "monto_original", type: "numeric", nullable: false),
                    montoactual = table.Column<decimal>(name: "monto_actual", type: "numeric", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acuerdos_comerciales_vias", x => x.id);
                    table.ForeignKey(
                        name: "FK_acuerdos_comerciales_vias_acuerdos_comerciales_acuerdo_come~",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                INSERT INTO acuerdos_comerciales_vias
                    (acuerdo_comercial_id, via_operacion, moneda_codigo, monto_original, monto_actual, estado, observaciones, fecha_alta, usuario_alta)
                SELECT
                    id,
                    via_operacion,
                    'ARS',
                    monto_total,
                    monto_total,
                    estado,
                    observaciones,
                    fecha_alta,
                    usuario_alta
                FROM acuerdos_comerciales
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM acuerdos_comerciales_vias v
                    WHERE v.acuerdo_comercial_id = acuerdos_comerciales.id
                      AND v.via_operacion = acuerdos_comerciales.via_operacion
                );
            ");

            migrationBuilder.AddColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "planes_pago",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "pagos_comerciales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moneda_codigo",
                table: "pagos_comerciales",
                type: "text",
                nullable: false,
                defaultValue: "ARS");

            migrationBuilder.AddColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "ajustes_cuotas_comerciales",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE planes_pago p
                SET acuerdo_comercial_via_id = v.id
                FROM acuerdos_comerciales_vias v
                WHERE v.acuerdo_comercial_id = p.acuerdo_comercial_id;

                UPDATE pagos_comerciales p
                SET acuerdo_comercial_via_id = v.id,
                    moneda_codigo = v.moneda_codigo
                FROM acuerdos_comerciales_vias v
                WHERE v.acuerdo_comercial_id = p.acuerdo_comercial_id;

                UPDATE ajustes_cuotas_comerciales a
                SET acuerdo_comercial_via_id = v.id
                FROM acuerdos_comerciales_vias v
                WHERE v.acuerdo_comercial_id = a.acuerdo_comercial_id;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "planes_pago",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "pagos_comerciales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "acuerdo_comercial_via_id",
                table: "ajustes_cuotas_comerciales",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ajustes_acuerdos_comerciales_vias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acuerdocomercialviaid = table.Column<int>(name: "acuerdo_comercial_via_id", type: "integer", nullable: false),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    viaoperacion = table.Column<int>(name: "via_operacion", type: "integer", nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "text", nullable: false),
                    montoanterior = table.Column<decimal>(name: "monto_anterior", type: "numeric", nullable: false),
                    montonuevo = table.Column<decimal>(name: "monto_nuevo", type: "numeric", nullable: false),
                    diferencia = table.Column<decimal>(type: "numeric", nullable: false),
                    tipoajuste = table.Column<int>(name: "tipo_ajuste", type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "text", nullable: false),
                    fechaajuste = table.Column<DateTime>(name: "fecha_ajuste", type: "timestamp with time zone", nullable: false),
                    usuarioajuste = table.Column<string>(name: "usuario_ajuste", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ajustes_acuerdos_comerciales_vias", x => x.id);
                    table.ForeignKey(
                        name: "FK_ajustes_acuerdos_comerciales_vias_acuerdos_comerciales_acue~",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ajustes_acuerdos_comerciales_vias_acuerdos_comerciales_vias~",
                        column: x => x.acuerdocomercialviaid,
                        principalTable: "acuerdos_comerciales_vias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_planes_pago_acuerdo_comercial_via_id",
                table: "planes_pago",
                column: "acuerdo_comercial_via_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comerciales_acuerdo_comercial_via_id",
                table: "pagos_comerciales",
                column: "acuerdo_comercial_via_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_cuotas_comerciales_acuerdo_comercial_via_id",
                table: "ajustes_cuotas_comerciales",
                column: "acuerdo_comercial_via_id");

            migrationBuilder.CreateIndex(
                name: "IX_acuerdos_comerciales_vias_acuerdo_comercial_id_via_operacion",
                table: "acuerdos_comerciales_vias",
                columns: new[] { "acuerdo_comercial_id", "via_operacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_acuerdos_comerciales_vias_acuerdo_comercial_id",
                table: "ajustes_acuerdos_comerciales_vias",
                column: "acuerdo_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_ajustes_acuerdos_comerciales_vias_acuerdo_comercial_via_id",
                table: "ajustes_acuerdos_comerciales_vias",
                column: "acuerdo_comercial_via_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ajustes_cuotas_comerciales_acuerdos_comerciales_vias_acuerd~",
                table: "ajustes_cuotas_comerciales",
                column: "acuerdo_comercial_via_id",
                principalTable: "acuerdos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pagos_comerciales_acuerdos_comerciales_vias_acuerdo_comerci~",
                table: "pagos_comerciales",
                column: "acuerdo_comercial_via_id",
                principalTable: "acuerdos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_planes_pago_acuerdos_comerciales_vias_acuerdo_comercial_via~",
                table: "planes_pago",
                column: "acuerdo_comercial_via_id",
                principalTable: "acuerdos_comerciales_vias",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ajustes_cuotas_comerciales_acuerdos_comerciales_vias_acuerd~",
                table: "ajustes_cuotas_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_pagos_comerciales_acuerdos_comerciales_vias_acuerdo_comerci~",
                table: "pagos_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_planes_pago_acuerdos_comerciales_vias_acuerdo_comercial_via~",
                table: "planes_pago");

            migrationBuilder.DropTable(
                name: "ajustes_acuerdos_comerciales_vias");

            migrationBuilder.DropTable(
                name: "acuerdos_comerciales_vias");

            migrationBuilder.DropIndex(
                name: "IX_planes_pago_acuerdo_comercial_via_id",
                table: "planes_pago");

            migrationBuilder.DropIndex(
                name: "IX_pagos_comerciales_acuerdo_comercial_via_id",
                table: "pagos_comerciales");

            migrationBuilder.DropIndex(
                name: "IX_ajustes_cuotas_comerciales_acuerdo_comercial_via_id",
                table: "ajustes_cuotas_comerciales");

            migrationBuilder.DropColumn(
                name: "acuerdo_comercial_via_id",
                table: "planes_pago");

            migrationBuilder.DropColumn(
                name: "acuerdo_comercial_via_id",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "moneda_codigo",
                table: "pagos_comerciales");

            migrationBuilder.DropColumn(
                name: "acuerdo_comercial_via_id",
                table: "ajustes_cuotas_comerciales");

            migrationBuilder.AlterColumn<int>(
                name: "acuerdo_comercial_id",
                table: "planes_pago",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_planes_pago_acuerdo_comercial_id",
                table: "planes_pago",
                column: "acuerdo_comercial_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_planes_pago_acuerdos_comerciales_acuerdo_comercial_id",
                table: "planes_pago",
                column: "acuerdo_comercial_id",
                principalTable: "acuerdos_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

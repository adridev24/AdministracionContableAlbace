using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class RechazoChequesTerceros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "asiento_contable_rechazo_id",
                table: "cheques_terceros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_rechazo",
                table: "cheques_terceros",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_rechazo",
                table: "cheques_terceros",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_rechazo",
                table: "cheques_terceros",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cobranzas_medios_pago_aplicaciones_facturas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranzamediopagoid = table.Column<int>(name: "cobranza_medio_pago_id", type: "integer", nullable: false),
                    cobranzaaplicacionfacturaid = table.Column<int>(name: "cobranza_aplicacion_factura_id", type: "integer", nullable: false),
                    importeaplicado = table.Column<decimal>(name: "importe_aplicado", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cobranzas_medios_pago_aplicaciones_facturas", x => x.id);
                    table.CheckConstraint("ck_cobranzas_medios_pago_aplicaciones_facturas_importe_positivo", "importe_aplicado > 0");
                    table.ForeignKey(
                        name: "FK_cobranzas_medios_pago_aplicaciones_facturas_cobranzas_aplic~",
                        column: x => x.cobranzaaplicacionfacturaid,
                        principalTable: "cobranzas_aplicaciones_facturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cobranzas_medios_pago_aplicaciones_facturas_cobranzas_medio~",
                        column: x => x.cobranzamediopagoid,
                        principalTable: "cobranzas_medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_asiento_rechazo_id",
                table: "cheques_terceros",
                column: "asiento_contable_rechazo_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_aplic_fact_aplicacion_id",
                table: "cobranzas_medios_pago_aplicaciones_facturas",
                column: "cobranza_aplicacion_factura_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_aplic_fact_medio_aplicacion",
                table: "cobranzas_medios_pago_aplicaciones_facturas",
                columns: new[] { "cobranza_medio_pago_id", "cobranza_aplicacion_factura_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cobranzas_medios_pago_aplic_fact_medio_id",
                table: "cobranzas_medios_pago_aplicaciones_facturas",
                column: "cobranza_medio_pago_id");

            migrationBuilder.AddForeignKey(
                name: "FK_cheques_terceros_asientos_contables_asiento_contable_rechaz~",
                table: "cheques_terceros",
                column: "asiento_contable_rechazo_id",
                principalTable: "asientos_contables",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cheques_terceros_asientos_contables_asiento_contable_rechaz~",
                table: "cheques_terceros");

            migrationBuilder.DropTable(
                name: "cobranzas_medios_pago_aplicaciones_facturas");

            migrationBuilder.DropIndex(
                name: "ix_cheques_terceros_asiento_rechazo_id",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "asiento_contable_rechazo_id",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "fecha_rechazo",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "motivo_rechazo",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "usuario_rechazo",
                table: "cheques_terceros");
        }
    }
}

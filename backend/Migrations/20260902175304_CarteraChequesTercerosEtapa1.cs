using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class CarteraChequesTercerosEtapa1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cuit_librador",
                table: "cobranzas_medios_pago",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_emision",
                table: "cobranzas_medios_pago",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "librador",
                table: "cobranzas_medios_pago",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cheques_terceros",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cobranzamediopagoid = table.Column<int>(name: "cobranza_medio_pago_id", type: "integer", nullable: false),
                    bancocobranzaid = table.Column<int>(name: "banco_cobranza_id", type: "integer", nullable: false),
                    numerocheque = table.Column<string>(name: "numero_cheque", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechaemision = table.Column<DateTime>(name: "fecha_emision", type: "timestamp with time zone", nullable: false),
                    fechavencimiento = table.Column<DateTime>(name: "fecha_vencimiento", type: "timestamp with time zone", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "character varying(10)", maxLength: 10, nullable: false),
                    librador = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cuitlibrador = table.Column<string>(name: "cuit_librador", type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true),
                    fechadeposito = table.Column<DateTime>(name: "fecha_deposito", type: "timestamp with time zone", nullable: true),
                    bancodestino = table.Column<string>(name: "banco_destino", type: "character varying(200)", maxLength: 200, nullable: true),
                    cuentadestino = table.Column<string>(name: "cuenta_destino", type: "character varying(100)", maxLength: 100, nullable: true),
                    usuariodeposito = table.Column<string>(name: "usuario_deposito", type: "character varying(100)", maxLength: 100, nullable: true),
                    fechaacreditacion = table.Column<DateTime>(name: "fecha_acreditacion", type: "timestamp with time zone", nullable: true),
                    usuarioacreditacion = table.Column<string>(name: "usuario_acreditacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cheques_terceros", x => x.id);
                    table.ForeignKey(
                        name: "FK_cheques_terceros_cobranzas_bancos_catalogo_banco_cobranza_id",
                        column: x => x.bancocobranzaid,
                        principalTable: "cobranzas_bancos_catalogo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cheques_terceros_cobranzas_medios_pago_cobranza_medio_pago_~",
                        column: x => x.cobranzamediopagoid,
                        principalTable: "cobranzas_medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_banco_id",
                table: "cheques_terceros",
                column: "banco_cobranza_id");

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_cobranza_medio_pago_id",
                table: "cheques_terceros",
                column: "cobranza_medio_pago_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_estado",
                table: "cheques_terceros",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_fecha_vencimiento",
                table: "cheques_terceros",
                column: "fecha_vencimiento");

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_moneda",
                table: "cheques_terceros",
                column: "moneda_codigo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "cuit_librador",
                table: "cobranzas_medios_pago");

            migrationBuilder.DropColumn(
                name: "fecha_emision",
                table: "cobranzas_medios_pago");

            migrationBuilder.DropColumn(
                name: "librador",
                table: "cobranzas_medios_pago");
        }
    }
}

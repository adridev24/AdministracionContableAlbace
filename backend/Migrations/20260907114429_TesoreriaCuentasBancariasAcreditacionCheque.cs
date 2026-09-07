using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class TesoreriaCuentasBancariasAcreditacionCheque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_modificacion",
                table: "cobranzas_bancos_catalogo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "usuario_modificacion",
                table: "cobranzas_bancos_catalogo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "asiento_contable_acreditacion_id",
                table: "cheques_terceros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cuenta_bancaria_empresa_id",
                table: "cheques_terceros",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cuentas_bancarias_empresa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bancoid = table.Column<int>(name: "banco_id", type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipocuenta = table.Column<string>(name: "tipo_cuenta", type: "character varying(50)", maxLength: 50, nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "character varying(10)", maxLength: 10, nullable: false),
                    numerocuenta = table.Column<string>(name: "numero_cuenta", type: "character varying(100)", maxLength: 100, nullable: false),
                    cbu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    aliascbu = table.Column<string>(name: "alias_cbu", type: "character varying(100)", maxLength: 100, nullable: true),
                    cuentacontableid = table.Column<int>(name: "cuenta_contable_id", type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_bancarias_empresa", x => x.id);
                    table.ForeignKey(
                        name: "FK_cuentas_bancarias_empresa_cobranzas_bancos_catalogo_banco_id",
                        column: x => x.bancoid,
                        principalTable: "cobranzas_bancos_catalogo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cuentas_bancarias_empresa_cuentas_contables_cuenta_contable~",
                        column: x => x.cuentacontableid,
                        principalTable: "cuentas_contables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 2,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 3,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 4,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 5,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 6,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 7,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 8,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 9,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 10,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 11,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 12,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 13,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "cobranzas_bancos_catalogo",
                keyColumn: "id",
                keyValue: 14,
                columns: new[] { "fecha_modificacion", "usuario_modificacion" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_asiento_acreditacion_id",
                table: "cheques_terceros",
                column: "asiento_contable_acreditacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_cheques_terceros_cuenta_bancaria_empresa_id",
                table: "cheques_terceros",
                column: "cuenta_bancaria_empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_empresa_activa",
                table: "cuentas_bancarias_empresa",
                column: "activa");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_empresa_banco_id",
                table: "cuentas_bancarias_empresa",
                column: "banco_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_empresa_banco_numero_moneda",
                table: "cuentas_bancarias_empresa",
                columns: new[] { "banco_id", "numero_cuenta", "moneda_codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_empresa_cuenta_contable_id",
                table: "cuentas_bancarias_empresa",
                column: "cuenta_contable_id");

            migrationBuilder.CreateIndex(
                name: "ix_cuentas_bancarias_empresa_moneda",
                table: "cuentas_bancarias_empresa",
                column: "moneda_codigo");

            migrationBuilder.AddForeignKey(
                name: "FK_cheques_terceros_asientos_contables_asiento_contable_acredi~",
                table: "cheques_terceros",
                column: "asiento_contable_acreditacion_id",
                principalTable: "asientos_contables",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cheques_terceros_cuentas_bancarias_empresa_cuenta_bancaria_~",
                table: "cheques_terceros",
                column: "cuenta_bancaria_empresa_id",
                principalTable: "cuentas_bancarias_empresa",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cheques_terceros_asientos_contables_asiento_contable_acredi~",
                table: "cheques_terceros");

            migrationBuilder.DropForeignKey(
                name: "FK_cheques_terceros_cuentas_bancarias_empresa_cuenta_bancaria_~",
                table: "cheques_terceros");

            migrationBuilder.DropTable(
                name: "cuentas_bancarias_empresa");

            migrationBuilder.DropIndex(
                name: "ix_cheques_terceros_asiento_acreditacion_id",
                table: "cheques_terceros");

            migrationBuilder.DropIndex(
                name: "ix_cheques_terceros_cuenta_bancaria_empresa_id",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "fecha_modificacion",
                table: "cobranzas_bancos_catalogo");

            migrationBuilder.DropColumn(
                name: "usuario_modificacion",
                table: "cobranzas_bancos_catalogo");

            migrationBuilder.DropColumn(
                name: "asiento_contable_acreditacion_id",
                table: "cheques_terceros");

            migrationBuilder.DropColumn(
                name: "cuenta_bancaria_empresa_id",
                table: "cheques_terceros");
        }
    }
}

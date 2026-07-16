using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesIibbPerceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_ultimo_calculo_percepcion",
                table: "ventas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "percepcion_iibb_requiere_recalculo",
                table: "ventas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "total_percepciones",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ventas_clientes_percepcion_iibb_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "character varying(50)", maxLength: 50, nullable: false),
                    situacion = table.Column<int>(type: "integer", nullable: false),
                    regimenpercepcioniibbid = table.Column<int>(name: "regimen_percepcion_iibb_id", type: "integer", nullable: true),
                    numeroinscripcioniibb = table.Column<string>(name: "numero_inscripcion_iibb", type: "character varying(50)", maxLength: 50, nullable: true),
                    jurisdiccioniibb = table.Column<string>(name: "jurisdiccion_iibb", type: "character varying(100)", maxLength: 100, nullable: true),
                    exclusiondesde = table.Column<DateTime>(name: "exclusion_desde", type: "timestamp with time zone", nullable: true),
                    exclusionhasta = table.Column<DateTime>(name: "exclusion_hasta", type: "timestamp with time zone", nullable: true),
                    motivoexclusion = table.Column<string>(name: "motivo_exclusion", type: "character varying(500)", maxLength: 500, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_clientes_percepcion_iibb_config", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_clientes_percepcion_iibb_config_ventas_percepciones_~",
                        column: x => x.regimenpercepcioniibbid,
                        principalTable: "ventas_percepciones_iibb",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas_percepciones_iibb_aplicadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ventaid = table.Column<int>(name: "venta_id", type: "integer", nullable: false),
                    regimenpercepcioniibbid = table.Column<int>(name: "regimen_percepcion_iibb_id", type: "integer", nullable: true),
                    codigoregimenaplicado = table.Column<string>(name: "codigo_regimen_aplicado", type: "character varying(50)", maxLength: 50, nullable: true),
                    descripcionregimenaplicada = table.Column<string>(name: "descripcion_regimen_aplicada", type: "character varying(250)", maxLength: 250, nullable: true),
                    jurisdiccionaplicada = table.Column<string>(name: "jurisdiccion_aplicada", type: "character varying(100)", maxLength: 100, nullable: true),
                    tipotributoaplicado = table.Column<string>(name: "tipo_tributo_aplicado", type: "character varying(50)", maxLength: 50, nullable: true),
                    numeroregimenaplicado = table.Column<string>(name: "numero_regimen_aplicado", type: "character varying(50)", maxLength: 50, nullable: true),
                    tipobasecalculo = table.Column<int>(name: "tipo_base_calculo", type: "integer", nullable: true),
                    baseimponible = table.Column<decimal>(name: "base_imponible", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    alicuotaaplicada = table.Column<decimal>(name: "alicuota_aplicada", type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    vigenciadesdeaplicada = table.Column<DateTime>(name: "vigencia_desde_aplicada", type: "timestamp with time zone", nullable: true),
                    vigenciahastaaplicada = table.Column<DateTime>(name: "vigencia_hasta_aplicada", type: "timestamp with time zone", nullable: true),
                    resultado = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    esautomatica = table.Column<bool>(name: "es_automatica", type: "boolean", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_percepciones_iibb_aplicadas", x => x.id);
                    table.ForeignKey(
                        name: "FK_ventas_percepciones_iibb_aplicadas_ventas_percepciones_iibb~",
                        column: x => x.regimenpercepcioniibbid,
                        principalTable: "ventas_percepciones_iibb",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_percepciones_iibb_aplicadas_ventas_venta_id",
                        column: x => x.ventaid,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_clientes_percepcion_iibb_cliente",
                table: "ventas_clientes_percepcion_iibb_config",
                column: "cliente_externo_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_clientes_percepcion_iibb_regimen",
                table: "ventas_clientes_percepcion_iibb_config",
                column: "regimen_percepcion_iibb_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_aplicadas_regimen",
                table: "ventas_percepciones_iibb_aplicadas",
                column: "regimen_percepcion_iibb_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_aplicadas_venta",
                table: "ventas_percepciones_iibb_aplicadas",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_aplicadas_venta_regimen_activa",
                table: "ventas_percepciones_iibb_aplicadas",
                columns: new[] { "venta_id", "regimen_percepcion_iibb_id", "activa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_clientes_percepcion_iibb_config");

            migrationBuilder.DropTable(
                name: "ventas_percepciones_iibb_aplicadas");

            migrationBuilder.DropColumn(
                name: "fecha_ultimo_calculo_percepcion",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "percepcion_iibb_requiere_recalculo",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_percepciones",
                table: "ventas");
        }
    }
}

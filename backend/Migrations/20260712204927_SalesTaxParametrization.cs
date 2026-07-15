using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesTaxParametrization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ventas_alicuotas_iva",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipotratamiento = table.Column<int>(name: "tipo_tratamiento", type: "integer", nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_alicuotas_iva", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ventas_nomencladores_fce",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_nomencladores_fce", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ventas_percepciones_iibb",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    jurisdiccion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Entre Rios"),
                    tipotributo = table.Column<string>(name: "tipo_tributo", type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "PERCEPCION_IIBB"),
                    numeroregimen = table.Column<string>(name: "numero_regimen", type: "character varying(50)", maxLength: 50, nullable: false),
                    porcentaje = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    tipobasecalculo = table.Column<int>(name: "tipo_base_calculo", type: "integer", nullable: false),
                    montominimo = table.Column<decimal>(name: "monto_minimo", type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    vigenciadesde = table.Column<DateTime>(name: "vigencia_desde", type: "timestamp with time zone", nullable: false),
                    vigenciahasta = table.Column<DateTime>(name: "vigencia_hasta", type: "timestamp with time zone", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_percepciones_iibb", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_alicuotas_iva_activo",
                table: "ventas_alicuotas_iva",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_alicuotas_iva_codigo",
                table: "ventas_alicuotas_iva",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_nomencladores_fce_activo",
                table: "ventas_nomencladores_fce",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_nomencladores_fce_codigo",
                table: "ventas_nomencladores_fce",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_iibb_codigo",
                table: "ventas_percepciones_iibb",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_iibb_equivalencia",
                table: "ventas_percepciones_iibb",
                columns: new[] { "jurisdiccion", "tipo_tributo", "numero_regimen", "tipo_base_calculo", "activo" });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_percepciones_iibb_vigencia",
                table: "ventas_percepciones_iibb",
                columns: new[] { "vigencia_desde", "vigencia_hasta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_alicuotas_iva");

            migrationBuilder.DropTable(
                name: "ventas_nomencladores_fce");

            migrationBuilder.DropTable(
                name: "ventas_percepciones_iibb");
        }
    }
}

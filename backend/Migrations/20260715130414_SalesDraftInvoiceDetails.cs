using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesDraftInvoiceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "neto_gravado",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_bruto",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_antes_percepciones",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_descuentos",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_exento",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_iva",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_no_gravado",
                table: "ventas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ventas_detalles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ventaid = table.Column<int>(name: "venta_id", type: "integer", nullable: false),
                    numerolinea = table.Column<int>(name: "numero_linea", type: "integer", nullable: false),
                    codigoitem = table.Column<string>(name: "codigo_item", type: "character varying(100)", maxLength: 100, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    preciounitario = table.Column<decimal>(name: "precio_unitario", type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    porcentajedescuento = table.Column<decimal>(name: "porcentaje_descuento", type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    importebruto = table.Column<decimal>(name: "importe_bruto", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    importedescuento = table.Column<decimal>(name: "importe_descuento", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    neto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tratamientoivaid = table.Column<int>(name: "tratamiento_iva_id", type: "integer", nullable: false),
                    tratamientoivacodigo = table.Column<string>(name: "tratamiento_iva_codigo", type: "character varying(50)", maxLength: 50, nullable: false),
                    tratamientoivadescripcion = table.Column<string>(name: "tratamiento_iva_descripcion", type: "character varying(200)", maxLength: 200, nullable: false),
                    tipotratamientoiva = table.Column<int>(name: "tipo_tratamiento_iva", type: "integer", nullable: false),
                    porcentajeivaaplicado = table.Column<decimal>(name: "porcentaje_iva_aplicado", type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    importeiva = table.Column<decimal>(name: "importe_iva", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    nomencladorid = table.Column<int>(name: "nomenclador_id", type: "integer", nullable: true),
                    nomencladorcodigo = table.Column<string>(name: "nomenclador_codigo", type: "character varying(50)", maxLength: 50, nullable: true),
                    nomencladordescripcion = table.Column<string>(name: "nomenclador_descripcion", type: "character varying(250)", maxLength: 250, nullable: true),
                    totallinea = table.Column<decimal>(name: "total_linea", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_detalles", x => x.id);
                    table.CheckConstraint("ck_ventas_detalles_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_ventas_detalles_descuento", "porcentaje_descuento >= 0 AND porcentaje_descuento <= 100");
                    table.CheckConstraint("ck_ventas_detalles_precio_unitario", "precio_unitario >= 0");
                    table.ForeignKey(
                        name: "FK_ventas_detalles_ventas_alicuotas_iva_tratamiento_iva_id",
                        column: x => x.tratamientoivaid,
                        principalTable: "ventas_alicuotas_iva",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_ventas_nomencladores_fce_nomenclador_id",
                        column: x => x.nomencladorid,
                        principalTable: "ventas_nomencladores_fce",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_ventas_venta_id",
                        column: x => x.ventaid,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_nomenclador_id",
                table: "ventas_detalles",
                column: "nomenclador_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_tratamiento_iva_id",
                table: "ventas_detalles",
                column: "tratamiento_iva_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_venta_id",
                table: "ventas_detalles",
                column: "venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_venta_linea",
                table: "ventas_detalles",
                columns: new[] { "venta_id", "numero_linea" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "neto_gravado",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "subtotal_bruto",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_antes_percepciones",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_descuentos",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_exento",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_iva",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "total_no_gravado",
                table: "ventas");
        }
    }
}

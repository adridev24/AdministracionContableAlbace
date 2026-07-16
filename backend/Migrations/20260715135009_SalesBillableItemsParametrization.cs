using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesBillableItemsParametrization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ventas_categorias_items_facturables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_categorias_items_facturables", x => x.id);
                    table.CheckConstraint("ck_ventas_categorias_items_orden", "orden >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ventas_unidades_medida",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    abreviatura = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    permitedecimales = table.Column<bool>(name: "permite_decimales", type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "character varying(100)", maxLength: 100, nullable: false),
                    fechamodificacion = table.Column<DateTime>(name: "fecha_modificacion", type: "timestamp with time zone", nullable: true),
                    usuariomodificacion = table.Column<string>(name: "usuario_modificacion", type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_unidades_medida", x => x.id);
                    table.CheckConstraint("ck_ventas_unidades_medida_orden", "orden >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ventas_items_facturables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcionampliada = table.Column<string>(name: "descripcion_ampliada", type: "character varying(1000)", maxLength: 1000, nullable: true),
                    categoriaitemfacturableid = table.Column<int>(name: "categoria_item_facturable_id", type: "integer", nullable: true),
                    unidadmedidaventaid = table.Column<int>(name: "unidad_medida_venta_id", type: "integer", nullable: false),
                    tratamientoivapredeterminadoid = table.Column<int>(name: "tratamiento_iva_predeterminado_id", type: "integer", nullable: false),
                    nomencladorpredeterminadoid = table.Column<int>(name: "nomenclador_predeterminado_id", type: "integer", nullable: true),
                    preciopredeterminado = table.Column<decimal>(name: "precio_predeterminado", type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
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
                    table.PrimaryKey("PK_ventas_items_facturables", x => x.id);
                    table.CheckConstraint("ck_ventas_items_facturables_orden", "orden >= 0");
                    table.CheckConstraint("ck_ventas_items_facturables_precio", "precio_predeterminado IS NULL OR precio_predeterminado >= 0");
                    table.ForeignKey(
                        name: "FK_ventas_items_facturables_ventas_alicuotas_iva_tratamiento_i~",
                        column: x => x.tratamientoivapredeterminadoid,
                        principalTable: "ventas_alicuotas_iva",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_items_facturables_ventas_categorias_items_facturable~",
                        column: x => x.categoriaitemfacturableid,
                        principalTable: "ventas_categorias_items_facturables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_items_facturables_ventas_nomencladores_fce_nomenclad~",
                        column: x => x.nomencladorpredeterminadoid,
                        principalTable: "ventas_nomencladores_fce",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_items_facturables_ventas_unidades_medida_unidad_medi~",
                        column: x => x.unidadmedidaventaid,
                        principalTable: "ventas_unidades_medida",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ventas_categorias_items_activo",
                table: "ventas_categorias_items_facturables",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_categorias_items_codigo",
                table: "ventas_categorias_items_facturables",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_activo",
                table: "ventas_items_facturables",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_categoria_id",
                table: "ventas_items_facturables",
                column: "categoria_item_facturable_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_codigo",
                table: "ventas_items_facturables",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_descripcion",
                table: "ventas_items_facturables",
                column: "descripcion");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_iva_id",
                table: "ventas_items_facturables",
                column: "tratamiento_iva_predeterminado_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_nomenclador_id",
                table: "ventas_items_facturables",
                column: "nomenclador_predeterminado_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_items_facturables_unidad_id",
                table: "ventas_items_facturables",
                column: "unidad_medida_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_unidades_medida_activo",
                table: "ventas_unidades_medida",
                column: "activo");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_unidades_medida_codigo",
                table: "ventas_unidades_medida",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_items_facturables");

            migrationBuilder.DropTable(
                name: "ventas_categorias_items_facturables");

            migrationBuilder.DropTable(
                name: "ventas_unidades_medida");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class SalesDetailBillableItemsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "categoria_item_facturable_codigo",
                table: "ventas_detalles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "categoria_item_facturable_descripcion",
                table: "ventas_detalles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "categoria_item_facturable_id",
                table: "ventas_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "item_facturable_descripcion",
                table: "ventas_detalles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "item_facturable_id",
                table: "ventas_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unidad_medida_abreviatura",
                table: "ventas_detalles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unidad_medida_codigo",
                table: "ventas_detalles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unidad_medida_descripcion",
                table: "ventas_detalles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unidad_medida_venta_id",
                table: "ventas_detalles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_categoria_item_facturable_id",
                table: "ventas_detalles",
                column: "categoria_item_facturable_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_item_facturable_id",
                table: "ventas_detalles",
                column: "item_facturable_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_unidad_medida_venta_id",
                table: "ventas_detalles",
                column: "unidad_medida_venta_id");

            migrationBuilder.CreateIndex(
                name: "ix_ventas_detalles_venta_item_facturable_id",
                table: "ventas_detalles",
                columns: new[] { "venta_id", "item_facturable_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_detalles_ventas_categorias_items_facturables_categor~",
                table: "ventas_detalles",
                column: "categoria_item_facturable_id",
                principalTable: "ventas_categorias_items_facturables",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_detalles_ventas_items_facturables_item_facturable_id",
                table: "ventas_detalles",
                column: "item_facturable_id",
                principalTable: "ventas_items_facturables",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_detalles_ventas_unidades_medida_unidad_medida_venta_~",
                table: "ventas_detalles",
                column: "unidad_medida_venta_id",
                principalTable: "ventas_unidades_medida",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_detalles_ventas_categorias_items_facturables_categor~",
                table: "ventas_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_ventas_detalles_ventas_items_facturables_item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropForeignKey(
                name: "FK_ventas_detalles_ventas_unidades_medida_unidad_medida_venta_~",
                table: "ventas_detalles");

            migrationBuilder.DropIndex(
                name: "ix_ventas_detalles_categoria_item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropIndex(
                name: "ix_ventas_detalles_item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropIndex(
                name: "ix_ventas_detalles_unidad_medida_venta_id",
                table: "ventas_detalles");

            migrationBuilder.DropIndex(
                name: "ix_ventas_detalles_venta_item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "categoria_item_facturable_codigo",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "categoria_item_facturable_descripcion",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "categoria_item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "item_facturable_descripcion",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "item_facturable_id",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "unidad_medida_abreviatura",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "unidad_medida_codigo",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "unidad_medida_descripcion",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "unidad_medida_venta_id",
                table: "ventas_detalles");
        }
    }
}

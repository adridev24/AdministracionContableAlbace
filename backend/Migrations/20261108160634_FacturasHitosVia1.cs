using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class FacturasHitosVia1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vinculaciones_facturas_hitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    HitoId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAlta = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculaciones_facturas_hitos", x => x.Id);
                    table.CheckConstraint("CK_factura_hito_importe", "\"Importe\" > 0");
                    table.ForeignKey(
                        name: "FK_vinculaciones_facturas_hitos_hitos_comerciales_vias_HitoId",
                        column: x => x.HitoId,
                        principalTable: "hitos_comerciales_vias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vinculaciones_facturas_hitos_ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vinculaciones_facturas_hitos_HitoId",
                table: "vinculaciones_facturas_hitos",
                column: "HitoId");

            migrationBuilder.CreateIndex(
                name: "IX_vinculaciones_facturas_hitos_VentaId_HitoId",
                table: "vinculaciones_facturas_hitos",
                columns: new[] { "VentaId", "HitoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vinculaciones_facturas_hitos");
        }
    }
}

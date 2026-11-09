using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReportesFiscalesVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reportes_empresa_fiscal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cuit = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    DomicilioFiscal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CondicionIva = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InscripcionIibb = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Actividad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InicioActividades = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualizadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoPor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes_empresa_fiscal", x => x.Id);
                    table.CheckConstraint("CK_reportes_empresa_unica", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "reportes_exportaciones_fiscales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Periodo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    GeneradoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneradoPor = table.Column<string>(type: "text", nullable: false),
                    NombreArchivo = table.Column<string>(type: "text", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    CantidadComprobantes = table.Column<int>(type: "integer", nullable: false),
                    Contenido = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes_exportaciones_fiscales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reportes_retenciones_fiscales",
                columns: table => new
                {
                    CobranzaMedioPagoId = table.Column<int>(type: "integer", nullable: false),
                    CuitAgente = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    RazonSocialAgente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Certificado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Regimen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Jurisdiccion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaRetencion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodoComputo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ActualizadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoPor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes_retenciones_fiscales", x => x.CobranzaMedioPagoId);
                    table.ForeignKey(
                        name: "FK_reportes_retenciones_fiscales_cobranzas_medios_pago_Cobranz~",
                        column: x => x.CobranzaMedioPagoId,
                        principalTable: "cobranzas_medios_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reportes_ventas_fiscales",
                columns: table => new
                {
                    VentaId = table.Column<int>(type: "integer", nullable: false),
                    TipoComprobanteArca = table.Column<int>(type: "integer", nullable: false),
                    TipoDocumento = table.Column<int>(type: "integer", nullable: false),
                    Documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CondicionIva = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CodigoOperacion = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    DevolucionChequeRevisada = table.Column<bool>(type: "boolean", nullable: false),
                    ActualizadoEl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualizadoPor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reportes_ventas_fiscales", x => x.VentaId);
                    table.ForeignKey(
                        name: "FK_reportes_ventas_fiscales_ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "ventas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reportes_exportaciones_fiscales_Periodo_GeneradoEl",
                table: "reportes_exportaciones_fiscales",
                columns: new[] { "Periodo", "GeneradoEl" });

            migrationBuilder.CreateIndex(
                name: "IX_reportes_retenciones_fiscales_PeriodoComputo",
                table: "reportes_retenciones_fiscales",
                column: "PeriodoComputo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reportes_empresa_fiscal");

            migrationBuilder.DropTable(
                name: "reportes_exportaciones_fiscales");

            migrationBuilder.DropTable(
                name: "reportes_retenciones_fiscales");

            migrationBuilder.DropTable(
                name: "reportes_ventas_fiscales");
        }
    }
}

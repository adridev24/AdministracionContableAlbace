using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class RevisionPlanPagoAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Abort before changing schema if legacy numbering cannot support the invariants.
            ValidateExistingData(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_ajustes_cuotas_comerciales_cuotas_comerciales_cuota_comerci~",
                table: "ajustes_cuotas_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_aplicaciones_pago_comerciales_cuotas_comerciales_cuota_come~",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_vinculaciones_factura_comerciales_cuotas_comerciales_cuota_~",
                table: "vinculaciones_factura_comerciales");

            migrationBuilder.DropIndex(
                name: "IX_cuotas_comerciales_plan_pago_id",
                table: "cuotas_comerciales");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "planes_pago",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "cuotas_comerciales",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "acuerdos_comerciales_vias",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_planes_pago_id_acuerdo_comercial_via_id",
                table: "planes_pago",
                columns: new[] { "id", "acuerdo_comercial_via_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_cuotas_comerciales_id_plan_pago_id",
                table: "cuotas_comerciales",
                columns: new[] { "id", "plan_pago_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_acuerdos_comerciales_vias_id_acuerdo_comercial_id",
                table: "acuerdos_comerciales_vias",
                columns: new[] { "id", "acuerdo_comercial_id" });

            migrationBuilder.CreateTable(
                name: "revisiones_planes_pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    solicitudid = table.Column<Guid>(name: "solicitud_id", type: "uuid", nullable: false),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    acuerdocomercialviaid = table.Column<int>(name: "acuerdo_comercial_via_id", type: "integer", nullable: false),
                    planpagoid = table.Column<int>(name: "plan_pago_id", type: "integer", nullable: false),
                    montoanterior = table.Column<decimal>(name: "monto_anterior", type: "numeric", nullable: false),
                    montonuevo = table.Column<decimal>(name: "monto_nuevo", type: "numeric", nullable: false),
                    diferencia = table.Column<decimal>(type: "numeric", nullable: false),
                    comentario = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    usuarioid = table.Column<string>(name: "usuario_id", type: "character varying(100)", maxLength: 100, nullable: false),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tiporevision = table.Column<int>(name: "tipo_revision", type: "integer", nullable: false),
                    monedacodigo = table.Column<string>(name: "moneda_codigo", type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisiones_planes_pago", x => x.id);
                    table.UniqueConstraint("AK_revisiones_planes_pago_id_plan_pago_id", x => new { x.id, x.planpagoid });
                    table.CheckConstraint("ck_revision_comentario", "length(btrim(comentario)) > 0");
                    table.CheckConstraint("ck_revision_moneda", "length(btrim(moneda_codigo)) > 0");
                    table.CheckConstraint("ck_revision_montos", "monto_anterior >= 0 AND monto_nuevo >= 0 AND diferencia = monto_nuevo - monto_anterior");
                    table.CheckConstraint("ck_revision_solicitud", "solicitud_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_revision_tipo", "tipo_revision IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_revision_usuario", "length(btrim(usuario_id)) > 0 AND length(btrim(usuario)) > 0");
                    table.ForeignKey(
                        name: "FK_revisiones_planes_pago_acuerdos_comerciales_acuerdo_comerci~",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revisiones_planes_pago_acuerdos_comerciales_vias_acuerdo_co~",
                        columns: x => new { x.acuerdocomercialviaid, x.acuerdocomercialid },
                        principalTable: "acuerdos_comerciales_vias",
                        principalColumns: new[] { "id", "acuerdo_comercial_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revisiones_planes_pago_planes_pago_plan_pago_id_acuerdo_com~",
                        columns: x => new { x.planpagoid, x.acuerdocomercialviaid },
                        principalTable: "planes_pago",
                        principalColumns: new[] { "id", "acuerdo_comercial_via_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "revisiones_planes_pago_detalles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    revisionplanpagoid = table.Column<int>(name: "revision_plan_pago_id", type: "integer", nullable: false),
                    planpagoid = table.Column<int>(name: "plan_pago_id", type: "integer", nullable: false),
                    cuotacomercialid = table.Column<int>(name: "cuota_comercial_id", type: "integer", nullable: true),
                    cuotaoriginalid = table.Column<int>(name: "cuota_original_id", type: "integer", nullable: false),
                    numerocuota = table.Column<int>(name: "numero_cuota", type: "integer", nullable: false),
                    tipocuota = table.Column<int>(name: "tipo_cuota", type: "integer", nullable: false),
                    operacion = table.Column<int>(type: "integer", nullable: false),
                    importeanterior = table.Column<decimal>(name: "importe_anterior", type: "numeric", nullable: true),
                    importenuevo = table.Column<decimal>(name: "importe_nuevo", type: "numeric", nullable: true),
                    vencimientoanterior = table.Column<DateTime>(name: "vencimiento_anterior", type: "timestamp with time zone", nullable: true),
                    vencimientonuevo = table.Column<DateTime>(name: "vencimiento_nuevo", type: "timestamp with time zone", nullable: true),
                    estadoanterior = table.Column<int>(name: "estado_anterior", type: "integer", nullable: true),
                    estadonuevo = table.Column<int>(name: "estado_nuevo", type: "integer", nullable: true),
                    importepagadoanterior = table.Column<decimal>(name: "importe_pagado_anterior", type: "numeric", nullable: true),
                    saldopendienteanterior = table.Column<decimal>(name: "saldo_pendiente_anterior", type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisiones_planes_pago_detalles", x => x.id);
                    table.CheckConstraint("ck_revision_detalle_estados", "(estado_anterior IS NULL OR estado_anterior IN (0, 1, 2, 3, 4)) AND (estado_nuevo IS NULL OR estado_nuevo IN (0, 1, 2, 3, 4))");
                    table.CheckConstraint("ck_revision_detalle_identidad", "cuota_original_id > 0 AND numero_cuota >= 0 AND (cuota_comercial_id IS NULL OR cuota_comercial_id = cuota_original_id)");
                    table.CheckConstraint("ck_revision_detalle_importes", "(importe_anterior IS NULL OR importe_anterior >= 0) AND (importe_nuevo IS NULL OR importe_nuevo >= 0) AND (importe_pagado_anterior IS NULL OR importe_pagado_anterior >= 0) AND (saldo_pendiente_anterior IS NULL OR saldo_pendiente_anterior >= 0)");
                    table.CheckConstraint("ck_revision_detalle_tipo", "tipo_cuota IN (0, 1, 2, 3, 4) AND operacion IN (1, 2, 3, 4)");
                    table.CheckConstraint("ck_revision_detalle_valores", "\n                    (operacion = 2 AND cuota_comercial_id IS NOT NULL AND importe_anterior IS NULL AND vencimiento_anterior IS NULL AND estado_anterior IS NULL AND importe_pagado_anterior IS NULL AND saldo_pendiente_anterior IS NULL AND importe_nuevo IS NOT NULL AND vencimiento_nuevo IS NOT NULL AND estado_nuevo IS NOT NULL)\n                    OR (operacion = 3 AND cuota_comercial_id IS NULL AND importe_anterior IS NOT NULL AND vencimiento_anterior IS NOT NULL AND estado_anterior IS NOT NULL AND importe_pagado_anterior IS NOT NULL AND importe_pagado_anterior = 0 AND saldo_pendiente_anterior IS NOT NULL AND importe_nuevo IS NULL AND vencimiento_nuevo IS NULL AND estado_nuevo IS NULL)\n                    OR (operacion IN (1, 4) AND cuota_comercial_id IS NOT NULL AND importe_anterior IS NOT NULL AND vencimiento_anterior IS NOT NULL AND estado_anterior IS NOT NULL AND importe_pagado_anterior IS NOT NULL AND saldo_pendiente_anterior IS NOT NULL AND importe_nuevo IS NOT NULL AND vencimiento_nuevo IS NOT NULL AND estado_nuevo IS NOT NULL AND (operacion <> 4 OR estado_nuevo = 4))");
                    table.ForeignKey(
                        name: "FK_revisiones_planes_pago_detalles_cuotas_comerciales_cuota_co~",
                        columns: x => new { x.cuotacomercialid, x.planpagoid },
                        principalTable: "cuotas_comerciales",
                        principalColumns: new[] { "id", "plan_pago_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_revisiones_planes_pago_detalles_revisiones_planes_pago_revi~",
                        columns: x => new { x.revisionplanpagoid, x.planpagoid },
                        principalTable: "revisiones_planes_pago",
                        principalColumns: new[] { "id", "plan_pago_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cuota_anticipo_activo",
                table: "cuotas_comerciales",
                column: "plan_pago_id",
                unique: true,
                filter: "tipo_cuota = 0 AND estado <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_comerciales_plan_pago_id_numero_cuota",
                table: "cuotas_comerciales",
                columns: new[] { "plan_pago_id", "numero_cuota" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_acuerdo_comercial_id_fecha",
                table: "revisiones_planes_pago",
                columns: new[] { "acuerdo_comercial_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_acuerdo_comercial_via_id_acuerdo_com~",
                table: "revisiones_planes_pago",
                columns: new[] { "acuerdo_comercial_via_id", "acuerdo_comercial_id" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_acuerdo_comercial_via_id_fecha",
                table: "revisiones_planes_pago",
                columns: new[] { "acuerdo_comercial_via_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_plan_pago_id_acuerdo_comercial_via_id",
                table: "revisiones_planes_pago",
                columns: new[] { "plan_pago_id", "acuerdo_comercial_via_id" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_plan_pago_id_fecha",
                table: "revisiones_planes_pago",
                columns: new[] { "plan_pago_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_solicitud_id",
                table: "revisiones_planes_pago",
                column: "solicitud_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_detalles_cuota_comercial_id_plan_pag~",
                table: "revisiones_planes_pago_detalles",
                columns: new[] { "cuota_comercial_id", "plan_pago_id" });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_detalles_cuota_original_id",
                table: "revisiones_planes_pago_detalles",
                column: "cuota_original_id");

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_detalles_revision_plan_pago_id_cuota~",
                table: "revisiones_planes_pago_detalles",
                columns: new[] { "revision_plan_pago_id", "cuota_original_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_planes_pago_detalles_revision_plan_pago_id_plan_~",
                table: "revisiones_planes_pago_detalles",
                columns: new[] { "revision_plan_pago_id", "plan_pago_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_ajustes_cuotas_comerciales_cuotas_comerciales_cuota_comerci~",
                table: "ajustes_cuotas_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_aplicaciones_pago_comerciales_cuotas_comerciales_cuota_come~",
                table: "aplicaciones_pago_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vinculaciones_factura_comerciales_cuotas_comerciales_cuota_~",
                table: "vinculaciones_factura_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            InstallInfrastructure(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveInfrastructure(migrationBuilder);
            migrationBuilder.DropForeignKey(
                name: "FK_ajustes_cuotas_comerciales_cuotas_comerciales_cuota_comerci~",
                table: "ajustes_cuotas_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_aplicaciones_pago_comerciales_cuotas_comerciales_cuota_come~",
                table: "aplicaciones_pago_comerciales");

            migrationBuilder.DropForeignKey(
                name: "FK_vinculaciones_factura_comerciales_cuotas_comerciales_cuota_~",
                table: "vinculaciones_factura_comerciales");

            migrationBuilder.DropTable(
                name: "revisiones_planes_pago_detalles");

            migrationBuilder.DropTable(
                name: "revisiones_planes_pago");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_planes_pago_id_acuerdo_comercial_via_id",
                table: "planes_pago");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_cuotas_comerciales_id_plan_pago_id",
                table: "cuotas_comerciales");

            migrationBuilder.DropIndex(
                name: "ix_cuota_anticipo_activo",
                table: "cuotas_comerciales");

            migrationBuilder.DropIndex(
                name: "IX_cuotas_comerciales_plan_pago_id_numero_cuota",
                table: "cuotas_comerciales");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_acuerdos_comerciales_vias_id_acuerdo_comercial_id",
                table: "acuerdos_comerciales_vias");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "planes_pago");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "cuotas_comerciales");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "acuerdos_comerciales_vias");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_comerciales_plan_pago_id",
                table: "cuotas_comerciales",
                column: "plan_pago_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ajustes_cuotas_comerciales_cuotas_comerciales_cuota_comerci~",
                table: "ajustes_cuotas_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_aplicaciones_pago_comerciales_cuotas_comerciales_cuota_come~",
                table: "aplicaciones_pago_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_vinculaciones_factura_comerciales_cuotas_comerciales_cuota_~",
                table: "vinculaciones_factura_comerciales",
                column: "cuota_comercial_id",
                principalTable: "cuotas_comerciales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

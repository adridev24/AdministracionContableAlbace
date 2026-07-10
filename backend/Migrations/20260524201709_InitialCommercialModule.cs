using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BudgetControl.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommercialModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acuerdos_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "text", nullable: false),
                    obraexternaid = table.Column<string>(name: "obra_externa_id", type: "text", nullable: false),
                    numeroacuerdo = table.Column<string>(name: "numero_acuerdo", type: "text", nullable: false),
                    fechaacuerdo = table.Column<DateTime>(name: "fecha_acuerdo", type: "timestamp with time zone", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    montototal = table.Column<decimal>(name: "monto_total", type: "numeric", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    viaoperacion = table.Column<int>(name: "via_operacion", type: "integer", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    fechaalta = table.Column<DateTime>(name: "fecha_alta", type: "timestamp with time zone", nullable: false),
                    usuarioalta = table.Column<string>(name: "usuario_alta", type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acuerdos_comerciales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes_referencia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    documento = table.Column<string>(type: "text", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes_referencia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "obras_referencia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    obraexternaid = table.Column<string>(name: "obra_externa_id", type: "text", nullable: false),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "text", nullable: false),
                    nombreobra = table.Column<string>(name: "nombre_obra", type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obras_referencia", x => x.id);
                });

            // migrationBuilder.CreateTable(
            //     name: "roles",
            //     columns: table => new
            //     {
            //         id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         name = table.Column<string>(type: "text", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_roles", x => x.id);
            //     });

            // migrationBuilder.CreateTable(
            //     name: "users",
            //     columns: table => new
            //     {
            //         id = table.Column<int>(type: "integer", nullable: false)
            //             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            //         username = table.Column<string>(type: "text", nullable: false),
            //         passwordhash = table.Column<string>(name: "password_hash", type: "text", nullable: false),
            //         fullname = table.Column<string>(name: "full_name", type: "text", nullable: true),
            //         email = table.Column<string>(type: "text", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_users", x => x.id);
            //     });

            migrationBuilder.CreateTable(
                name: "pagos_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    clienteexternoid = table.Column<string>(name: "cliente_externo_id", type: "text", nullable: false),
                    obraexternaid = table.Column<string>(name: "obra_externa_id", type: "text", nullable: false),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    fechapago = table.Column<DateTime>(name: "fecha_pago", type: "timestamp with time zone", nullable: false),
                    importetotal = table.Column<decimal>(name: "importe_total", type: "numeric", nullable: false),
                    mediopago = table.Column<string>(name: "medio_pago", type: "text", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos_comerciales", x => x.id);
                    table.ForeignKey(
                        name: "FK_pagos_comerciales_acuerdos_comerciales_acuerdo_comercial_id",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "planes_pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acuerdocomercialid = table.Column<int>(name: "acuerdo_comercial_id", type: "integer", nullable: false),
                    tieneanticipo = table.Column<bool>(name: "tiene_anticipo", type: "boolean", nullable: false),
                    montoanticipo = table.Column<decimal>(name: "monto_anticipo", type: "numeric", nullable: false),
                    cantidadcuotas = table.Column<int>(name: "cantidad_cuotas", type: "integer", nullable: false),
                    fechaprimervencimiento = table.Column<DateTime>(name: "fecha_primer_vencimiento", type: "timestamp with time zone", nullable: false),
                    periodicidad = table.Column<string>(type: "text", nullable: false),
                    observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planes_pago", x => x.id);
                    table.ForeignKey(
                        name: "FK_planes_pago_acuerdos_comerciales_acuerdo_comercial_id",
                        column: x => x.acuerdocomercialid,
                        principalTable: "acuerdos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // migrationBuilder.CreateTable(
            //     name: "user_roles",
            //     columns: table => new
            //     {
            //         userid = table.Column<int>(name: "user_id", type: "integer", nullable: false),
            //         roleid = table.Column<int>(name: "role_id", type: "integer", nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_user_roles", x => new { x.userid, x.roleid });
            //         table.ForeignKey(
            //             name: "FK_user_roles_roles_role_id",
            //             column: x => x.roleid,
            //             principalTable: "roles",
            //             principalColumn: "id",
            //             onDelete: ReferentialAction.Cascade);
            //         table.ForeignKey(
            //             name: "FK_user_roles_users_user_id",
            //             column: x => x.userid,
            //             principalTable: "users",
            //             principalColumn: "id",
            //             onDelete: ReferentialAction.Cascade);
            //     });

            migrationBuilder.CreateTable(
                name: "cuotas_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    planpagoid = table.Column<int>(name: "plan_pago_id", type: "integer", nullable: false),
                    numerocuota = table.Column<int>(name: "numero_cuota", type: "integer", nullable: false),
                    tipocuota = table.Column<int>(name: "tipo_cuota", type: "integer", nullable: false),
                    fechavencimiento = table.Column<DateTime>(name: "fecha_vencimiento", type: "timestamp with time zone", nullable: false),
                    importeoriginal = table.Column<decimal>(name: "importe_original", type: "numeric", nullable: false),
                    importepagado = table.Column<decimal>(name: "importe_pagado", type: "numeric", nullable: false),
                    saldopendiente = table.Column<decimal>(name: "saldo_pendiente", type: "numeric", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuotas_comerciales", x => x.id);
                    table.ForeignKey(
                        name: "FK_cuotas_comerciales_planes_pago_plan_pago_id",
                        column: x => x.planpagoid,
                        principalTable: "planes_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aplicaciones_pago_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pagocomercialid = table.Column<int>(name: "pago_comercial_id", type: "integer", nullable: false),
                    cuotacomercialid = table.Column<int>(name: "cuota_comercial_id", type: "integer", nullable: false),
                    importeaplicado = table.Column<decimal>(name: "importe_aplicado", type: "numeric", nullable: false),
                    fechaaplicacion = table.Column<DateTime>(name: "fecha_aplicacion", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aplicaciones_pago_comerciales", x => x.id);
                    table.ForeignKey(
                        name: "FK_aplicaciones_pago_comerciales_cuotas_comerciales_cuota_come~",
                        column: x => x.cuotacomercialid,
                        principalTable: "cuotas_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_aplicaciones_pago_comerciales_pagos_comerciales_pago_comerc~",
                        column: x => x.pagocomercialid,
                        principalTable: "pagos_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vinculaciones_factura_comerciales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuotacomercialid = table.Column<int>(name: "cuota_comercial_id", type: "integer", nullable: false),
                    facturaexternaid = table.Column<string>(name: "factura_externa_id", type: "text", nullable: false),
                    numerofactura = table.Column<string>(name: "numero_factura", type: "text", nullable: false),
                    importevinculado = table.Column<decimal>(name: "importe_vinculado", type: "numeric", nullable: false),
                    fechavinculacion = table.Column<DateTime>(name: "fecha_vinculacion", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculaciones_factura_comerciales", x => x.id);
                    table.ForeignKey(
                        name: "FK_vinculaciones_factura_comerciales_cuotas_comerciales_cuota_~",
                        column: x => x.cuotacomercialid,
                        principalTable: "cuotas_comerciales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aplicaciones_pago_comerciales_cuota_comercial_id",
                table: "aplicaciones_pago_comerciales",
                column: "cuota_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_aplicaciones_pago_comerciales_pago_comercial_id",
                table: "aplicaciones_pago_comerciales",
                column: "pago_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_comerciales_plan_pago_id",
                table: "cuotas_comerciales",
                column: "plan_pago_id");

            migrationBuilder.CreateIndex(
                name: "IX_pagos_comerciales_acuerdo_comercial_id",
                table: "pagos_comerciales",
                column: "acuerdo_comercial_id");

            migrationBuilder.CreateIndex(
                name: "IX_planes_pago_acuerdo_comercial_id",
                table: "planes_pago",
                column: "acuerdo_comercial_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_vinculaciones_factura_comerciales_cuota_comercial_id",
                table: "vinculaciones_factura_comerciales",
                column: "cuota_comercial_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aplicaciones_pago_comerciales");

            migrationBuilder.DropTable(
                name: "clientes_referencia");

            migrationBuilder.DropTable(
                name: "obras_referencia");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "vinculaciones_factura_comerciales");

            migrationBuilder.DropTable(
                name: "pagos_comerciales");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "cuotas_comerciales");

            migrationBuilder.DropTable(
                name: "planes_pago");

            migrationBuilder.DropTable(
                name: "acuerdos_comerciales");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace BudgetControl.Api.Migrations
{
    public partial class RevisionPlanPagoAudit
    {
        private static void ValidateExistingData(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM cuotas_comerciales GROUP BY plan_pago_id, numero_cuota HAVING count(*) > 1)
                    OR EXISTS (SELECT 1 FROM cuotas_comerciales WHERE tipo_cuota = 0 AND estado <> 4 GROUP BY plan_pago_id HAVING count(*) > 1)
                THEN RAISE EXCEPTION 'Revisar numeración/anticipos duplicados antes de instalar RevisionPlanPagoAudit.';
                END IF;
            END $$;
            """);

        private static void InstallInfrastructure(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
            -- A durable tombstone of dependency existence, not a financial movement.
            -- Kept outside the EF entity graph so recording a dependency never changes
            -- a parent's xmin behind SaveChanges (which would break existing writers).
            CREATE TABLE cuotas_comerciales_dependencias_historicas (
                cuota_comercial_id integer PRIMARY KEY REFERENCES cuotas_comerciales(id) ON DELETE RESTRICT,
                primera_deteccion timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            INSERT INTO cuotas_comerciales_dependencias_historicas(cuota_comercial_id)
            SELECT cuota_comercial_id FROM aplicaciones_pago_comerciales WHERE cuota_comercial_id IS NOT NULL
            UNION SELECT cuota_comercial_id FROM vinculaciones_factura_comerciales
            UNION SELECT cuota_comercial_id FROM cobranzas_aplicaciones_obligaciones
            UNION SELECT cuota_comercial_id FROM ajustes_cuotas_comerciales;

            CREATE FUNCTION revision_plan_auditoria_inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'La auditoría de revisión y las evidencias de dependencias son inmutables.' USING ERRCODE = '23514';
            END $$;

            CREATE TRIGGER tr_revision_inmutable BEFORE UPDATE OR DELETE OR TRUNCATE ON revisiones_planes_pago
                FOR EACH STATEMENT EXECUTE FUNCTION revision_plan_auditoria_inmutable();
            CREATE TRIGGER tr_revision_detalle_inmutable BEFORE UPDATE OR DELETE OR TRUNCATE ON revisiones_planes_pago_detalles
                FOR EACH STATEMENT EXECUTE FUNCTION revision_plan_auditoria_inmutable();
            CREATE TRIGGER tr_cuota_historia_inmutable BEFORE UPDATE OR DELETE OR TRUNCATE ON cuotas_comerciales_dependencias_historicas
                FOR EACH STATEMENT EXECUTE FUNCTION revision_plan_auditoria_inmutable();

            CREATE FUNCTION revision_plan_registrar_dependencia() RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE
                ids integer[];
                cuota_id integer;
                version_leida xid;
                version_bloqueada xid;
            BEGIN
                IF TG_OP = 'INSERT' THEN ids := ARRAY[NEW.cuota_comercial_id];
                ELSIF TG_OP = 'DELETE' THEN ids := ARRAY[OLD.cuota_comercial_id];
                ELSE ids := ARRAY[OLD.cuota_comercial_id, NEW.cuota_comercial_id]; END IF;
                FOR cuota_id IN SELECT DISTINCT unnest(ids) ORDER BY 1 LOOP
                    IF cuota_id IS NULL THEN CONTINUE; END IF;
                    SELECT xmin INTO version_leida FROM cuotas_comerciales WHERE id = cuota_id;
                    SELECT xmin INTO version_bloqueada FROM cuotas_comerciales WHERE id = cuota_id FOR UPDATE;
                    IF version_leida IS DISTINCT FROM version_bloqueada THEN
                        RAISE EXCEPTION 'La cuota cambió mientras se incorporaba un movimiento; recargue y revalide.' USING ERRCODE = '40001';
                    END IF;
                    INSERT INTO cuotas_comerciales_dependencias_historicas(cuota_comercial_id)
                        VALUES (cuota_id) ON CONFLICT DO NOTHING;
                END LOOP;
                IF TG_OP = 'DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;
            END $$;

            CREATE TRIGGER tr_revision_dependencia BEFORE INSERT OR UPDATE OR DELETE ON aplicaciones_pago_comerciales
                FOR EACH ROW EXECUTE FUNCTION revision_plan_registrar_dependencia();
            CREATE TRIGGER tr_revision_dependencia BEFORE INSERT OR UPDATE OR DELETE ON vinculaciones_factura_comerciales
                FOR EACH ROW EXECUTE FUNCTION revision_plan_registrar_dependencia();
            CREATE TRIGGER tr_revision_dependencia BEFORE INSERT OR UPDATE OR DELETE ON cobranzas_aplicaciones_obligaciones
                FOR EACH ROW EXECUTE FUNCTION revision_plan_registrar_dependencia();
            CREATE TRIGGER tr_revision_dependencia BEFORE INSERT OR UPDATE OR DELETE ON ajustes_cuotas_comerciales
                FOR EACH ROW EXECUTE FUNCTION revision_plan_registrar_dependencia();
            CREATE TRIGGER tr_revision_dependencia BEFORE INSERT ON revisiones_planes_pago_detalles
                FOR EACH ROW EXECUTE FUNCTION revision_plan_registrar_dependencia();
            """);

        private static void RemoveInfrastructure(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
            DROP TRIGGER tr_revision_dependencia ON revisiones_planes_pago_detalles;
            DROP TRIGGER tr_revision_dependencia ON ajustes_cuotas_comerciales;
            DROP TRIGGER tr_revision_dependencia ON cobranzas_aplicaciones_obligaciones;
            DROP TRIGGER tr_revision_dependencia ON vinculaciones_factura_comerciales;
            DROP TRIGGER tr_revision_dependencia ON aplicaciones_pago_comerciales;
            DROP TRIGGER tr_revision_inmutable ON revisiones_planes_pago;
            DROP TRIGGER tr_revision_detalle_inmutable ON revisiones_planes_pago_detalles;
            DROP FUNCTION revision_plan_registrar_dependencia();
            DROP TABLE cuotas_comerciales_dependencias_historicas;
            DROP FUNCTION revision_plan_auditoria_inmutable();
            """);
    }
}

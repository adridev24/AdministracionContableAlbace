# Plan revision infrastructure (block 1)

Run from the repository root:

```powershell
dotnet run --project tests/PlanRevision.Tests -- backend/appsettings.json
dotnet run --project tests/CommercialCalendar.Tests -- backend/appsettings.json
```

The new executable follows the existing assertion-runner convention without new
packages. It accepts only localhost PostgreSQL and requires CREATEDB. It reads
credentials, connects to the maintenance database, creates a randomly named
`albace_revision_test_*` database, and removes exactly that database in finally.
It never starts the API, runs DbInitializer, or migrates the configured database.
The previous migration model supplies the baseline schema; then the real new
migration Up/Down operations run against the disposable database. This avoids
depending on historical bootstrap migrations that assume pre-existing tables.

Coverage includes EF/snapshot parity, xmin stale writes on all three entities,
audit constraints/roundtrip/immutability, cross-plan FK protection, physical-delete
snapshots, historical dependencies, cancelled-payment backfill, indexing, DDL and
data rollback, upgrade/downgrade, and two-connection contention with lock_timeout.

## Concurrency contract for the next block

Use a fresh AppDbContext and an explicit ReadCommitted transaction. Call
`PlanRevisionLock.AcquireAsync`, compare expected via/plan/installment xmin values,
then re-read all dependencies and states before validating the proposal. The
current collection of installment IDs must also match the submitted baseline:
xmin of a parent does not change merely because a child is inserted/deleted.
Hold the locks through all changes, audit, SaveChanges and commit. Never validate
before locking or reuse previously tracked entities. Acquire in via, plan, quota
ID order. Future endpoint maps concurrency/deadlock/serialization failures to a
controlled conflict with rollback and mandatory reload, not a silent replay.

xmin is an opaque PostgreSQL transaction token, not an audit revision number.
The lock helper serializes revisions and conflicts with FK key-share locks from
new payments, installments and dependencies. Migration triggers lock quotas for
dependency writes and reject a dependency that waited while its quota changed.
No endpoint uses this helper yet: approved revision behavior is intentionally
not enabled by this block. Existing callers still need their own complete
domain validations; schema locks do not infer whether a financial amount is valid.

## Historical dependencies and migration review

`cuotas_comerciales_dependencias_historicas` is SQL infrastructure, deliberately
outside the EF tracked graph. Its restrictive FK and immutable row retain proof
of any dependency even when a draft link is subsequently removed. Four existing
direct-dependency tables and new live audit details record this evidence. Backfill
includes cancelled rows. Links erased before installation cannot be recovered
from current data: reconcile them from backups/history before allowing deletion.
No paid amount or parent xmin is changed by evidence recording.

The migration replaces only the three incoming quota cascades with RESTRICT;
the existing collections RESTRICT remains. Audit FKs are RESTRICT and composite
keys enforce agreement/via/plan/quota consistency. Deleted detail rows use a
historical ID without FK and a null live reference. A live audited quota cannot
later be physically deleted. No remaining installment is renumbered.

Down destroys new audit/evidence tables and is for isolated verification or a
reviewed rollback only; never downgrade an installation with valuable audit data.
Production migration application, legacy missing FKs, final admin authorization,
tail-only withdrawal, confirmed summary, monetary equality, dynamic credit,
and report updates are outside this block. No changes to providers or settings.

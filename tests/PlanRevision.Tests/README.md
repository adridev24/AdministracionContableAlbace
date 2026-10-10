# Plan revision infrastructure and backend operation (blocks 1 and 2)

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

## Concurrency contract

Use a fresh AppDbContext and an explicit ReadCommitted transaction. Call
`PlanRevisionLock.AcquireAsync`, compare expected via/plan/installment xmin values,
then re-read all dependencies and states before validating the proposal. The
current collection of installment IDs must also match the submitted baseline:
xmin of a parent does not change merely because a child is inserted/deleted.
Hold the locks through all changes, audit, SaveChanges and commit. Never validate
before locking or reuse previously tracked entities. Acquire in via, plan, quota
ID order. The revision endpoint maps concurrency/deadlock/serialization failures to a
controlled conflict with rollback and mandatory reload, not a silent replay.

xmin is an opaque PostgreSQL transaction token, not an audit revision number.
The lock helper serializes revisions and conflicts with FK key-share locks from
new payments, installments and dependencies. Migration triggers lock quotas for
dependency writes and reject a dependency that waited while its quota changed.
`RevisionPlanPagoService` uses this helper with a transaction-local five-second
lock timeout. Existing callers still need their own complete
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
Production migration application, reconciliation of missing historical evidence,
frontend and report updates remain outside this block. No changes to providers or settings.

## Backend contract for block 3

Both endpoints require the real JWT role `Admin`:

- GET `/api/comercial/acuerdos-vias/{viaId}/plan-pago/revision?acuerdoId={id}&planId={id}`
- POST `/api/comercial/acuerdos-vias/{viaId}/plan-pago/revision`

GET returns the current plan, currency, original/current/total amounts, valid paid
total, pending balance and credit, aggregate and quota xmin versions, complete
ordered quota collection, historical-dependency flags, per-operation blocking
reasons, active ordinary tail ID and safe next number (including audit history).
Preparation uses a consistent read snapshot; it is not permission to mutate later.

POST accepts `RevisionPlanPagoRequest`: agreement/via/plan IDs, new current amount,
nonblank comment, nonempty request UUID, explicit confirmation, via/plan versions,
the complete original quota ID/version collection, and explicit changes. Existing
quotas require ID/version; modification accepts amount and/or due date; retirement
accepts neither. Additions require ordinary type, number, amount and due date.
Do not submit previous amounts, audit user, date or differences. Server values and
authenticated claims are authoritative. Money uses decimal, two fractional digits,
and exact equality with the sum of all non-annulled obligations. Calendar dates use
the existing CommercialCalendar normalization.

The operation validates every quota under via/plan/ordered-quota locks, processes
ordinary retirements in descending order, treats the advance separately, and keeps
non-ordinary obligations out of that sequence. Refuerzos/ajustes/adicionales remain
in the total but cannot be edited or created by this endpoint. Active payments,
applications, collections (including drafts) and invoices block changes. Unknown
invoice references also block; absence of an identifiable invoice is not annulment.
Cancelled dependencies remain history and force logical exclusion (`Anulada`). A
free quota can be physically removed with a complete audit snapshot and null live
reference. No renumbering occurs. Additions use the next number above both current
rows and audit snapshots, even after physical deletion.

Via1 paid totals use confirmed collection allocations once, excluding mirrored
commercial payments. Via2 uses non-annulled commercial-origin payment headers once,
never payment headers plus their applications. Incompatible currencies are rejected.
Credit/pending balances are dynamic max differences, not persisted movements.

Confirmation returns immutable audit plus current preparation state. Repeating a
request UUID for the same plan and authenticated user returns the original audit
without repeating changes; the accompanying balances are current. Reusing that UUID
with a different plan/user conflicts. Preserve the UUID when retrying a response lost
in transit; use a new UUID for a different operation. Stale versions/collections,
lock timeout, deadlock and concurrent dependency writes return 409 `{ error }`.
Reload before a new operation; do not silently replay a stale proposal.

The runner includes live HTTP/JWT tests on an ephemeral loopback port, standalone
from API Program/DbInitializer, and transactional service tests on the disposable DB.
It tests rollback after the first SaveChanges by injecting an audit failure with
PostgreSQL deadlock SQLSTATE; this checks error mapping, not the deadlock detector.

## Existing adjustment endpoint

POST `/api/comercial/planes/{planPagoId}/cuotas-ajuste` now requires `Admin` both
in MVC authorization and inside ComercialService. It exclusively accepts the
existing `Adicional`, `Refuerzo`, and `Ajuste` enum values. `Cuota` must use the
revision endpoint; `Anticipo`, omitted types and undefined enum values are rejected.
An authenticated non-admin receives 403, never 409. The service obtains the audit
user from authenticated claims, requires a motive and validates amount/date even
when called directly. Client state and audit-user fields are not authoritative.

Adjustments retain their existing AjusteCuotaComercial/AjusteAcuerdoComercialVia
audit and do not create RevisionPlanPago. Their positive amount increases
MontoActual without changing MontoOriginal. The full active-obligation total must
match MontoActual before and after the addition; existing discrepancies require
reconciliation. Number allocation also respects physically deleted audit history.
Tests cover the three allowed types, direct-service and HTTP authorization, numeric
and forged-state bypass attempts, amount consistency and ordinary revision entry.

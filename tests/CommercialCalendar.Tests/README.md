# Commercial calendar regression tests

Run from the repository root:

```powershell
dotnet run --no-restore --project tests/CommercialCalendar.Tests -- backend/appsettings.json
node --test frontend/src/shared/utils/calendarDate.test.js
```

The backend executable exits nonzero on a failed assertion. It uses the existing
API project and dependencies; no additional test packages are required.

PostgreSQL integration requires access to the configured database and permission
to create temporary tables. It clones only the structure of the commercial tables,
restricts the session search path to `pg_temp`, and runs the real service and EF
Core persistence against those temporary tables. It rolls back the transaction;
public table data is not modified. Closing the connection also removes temporary
objects if an assertion fails. It does not start the API or run migrations.

Coverage: independent advance/installment dates, conditional validation, amounts
and rounding, EF save/reload, disable/reactivate with identity preservation,
payment protections, overdue queries, Argentina business-day boundaries and
unchanged partial/paid/annulled states. Also covers first activation from a plan
without an advance, missing/minimum dates, conflicting duplicate dates, repeated
saves, explicit reactivation dates, and header-only updates of partial/paid advances.
Manual UI check: toggle the advance on a plan without a prior row, verify the date
field appears immediately, try saving empty, then select an independent date;
toggle off/on and verify the saved date is preserved until confirmation.
Frontend tests cover invalid inputs,
save/edit cycles and formatting under three time zones.

## Commercial report

`CommercialReportTests.cs` is included automatically by this executable project.
It covers General/Periodo, incomplete and inverted ranges, inclusive limits,
current debt versus receipts in the period, both active payment sources,
closed/annulled agreements and vias, partial advances, cancelled obligations,
currency-specific counts, ten debtors per currency and read-only GET behavior.
A context that throws on SaveChanges and before/after persisted snapshots detect
report writes. Collection tables are also temporary; no public data is changed.

Run the frontend report suite without additional packages:

```powershell
node --test frontend/src/modules/comercial/services/reportFilters.test.js
```

The frontend suite exercises request construction and applied-filter snapshots.
Manual browser verification still recommended: open the report, confirm no dates
in the initial request, select Periodo, reject missing/inverted dates, apply a
valid range, edit the draft without applying and confirm the result caption stays
unchanged. Return to General and confirm dates clear while the via is retained.
Verify the per-currency cards and the current-situation table descriptions.
These are not browser automation tests.

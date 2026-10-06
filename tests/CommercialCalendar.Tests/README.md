# Commercial calendar regression tests

Run from the repository root:

```powershell
dotnet run --project tests/CommercialCalendar.Tests -- backend/appsettings.json
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

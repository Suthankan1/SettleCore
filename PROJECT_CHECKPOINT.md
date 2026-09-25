# SettleCore checkpoint

## Completed

- 009.2A: Reconciliation module registered in the API (`0ad5b82`).
- 009.2B: `POST /reconciliations` happy path returns `201 Created`, normalized comparison data, and a location. An API integration test verifies the handler persists the same record.
- 009.2C: Invalid reconciliation currency maps to HTTP 400, with no record persisted.
- 009.2D: API integration cases cover all four comparison statuses and currency normalization.
- 009.3A: API → handler → EF repository → PostgreSQL creation verified through an independent database read.
- 009.3B: Repository can retrieve a reconciliation by ID or return null for a missing ID; PostgreSQL integration test covers both.
- 009.3C: Application query handler returns reconciliation details or null and is registered in DI.
- 009.3D: `GET /reconciliations/{id}` returns the record or HTTP 404; API tests cover both.
- 009.3E: Create then GET round-trips through PostgreSQL, with response checked against the persisted row.

## Next

- Next backend slice: choose ledger/audit foundation after reviewing the current schema and prior project scope.
- Continue backend features in small slices before frontend, deployment, and polish.

## Verification and repository state

- Baseline before 009.2B: `dotnet test SettleCore.slnx` passed (95 tests).
- After 009.2B: `dotnet test SettleCore.slnx` passed (96 tests).
- After 009.2C: `dotnet test SettleCore.slnx --nologo -v:q` passed (99 tests).
- After 009.2D: `dotnet test SettleCore.slnx --nologo -v:q` passed (103 tests).
- After 009.3A: `dotnet test SettleCore.slnx --nologo -v:q` passed (104 tests).
- After 009.3B: `dotnet test SettleCore.slnx --nologo -v:q` passed (105 tests).
- After 009.3C: `dotnet test SettleCore.slnx --nologo -v:q` passed (107 tests).
- After 009.3D: `dotnet test SettleCore.slnx --nologo -v:q` passed (109 tests).
- After 009.3E: `dotnet test SettleCore.slnx --nologo -v:q` passed (109 tests).
- The latest commit and push state should be checked with `git status --short --branch` and `git log -1 --oneline` when resuming.

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
- 010.1A: Ledger domain foundation has immutable entries in minor units and rejects transactions that are not balanced per currency.
- 010.1B: Ledger account model captures ID, ledger ownership, and normalized currency.

## Next

- 010.1C: Enforce account currency and ledger ownership before posting.
- 010.2: Persist ledger transactions and entries atomically; add PostgreSQL tests, then application and API slices.
- Later: payment-to-ledger integration, audit trail, provider ingestion, webhooks, workers/outbox/idempotency, observability, deployment, then frontend and polish.
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
- After 010.1A: `dotnet test SettleCore.slnx --nologo -v:q` passed (115 tests).
- After 010.1B: `dotnet test SettleCore.slnx --nologo -v:q` passed (120 tests).
- The latest commit and push state should be checked with `git status --short --branch` and `git log -1 --oneline` when resuming.

## Autonomous continuation — 2026-09-26

- Verified 010.5B was already implemented and pushed: `e76972d` on main and origin/main. Focused endpoint test passed; all 133 tests passed; build had no warnings/errors.
- 010.5C: Ledger ArgumentException failures now return HTTP 400 validation problems, matching Reconciliation. Added unbalanced-entry rejection test asserting no persistence; observed RED then GREEN. All 134 tests passed; build had no warnings/errors.
- Architecture preserved: endpoint maps requests to application commands; domain validates ledger invariants; no new dependencies or infrastructure.
- Next: malformed Ledger request coverage, then API-to-PostgreSQL persistence verification and transaction retrieval slices. Later backend scope remains as listed above.
- Capacity at last check: 76% five-hour / 20% weekly remaining. Stop around 10% remaining in either window.
- 010.5C pushed as `0a0e381`.
- 010.5D: Missing entries, explicit null entries, and null entry items return HTTP 400 validation problems without persistence. Three regression cases observed RED then GREEN; all 137 tests pass, build clean. Request-shape checks stay at the API boundary.
- Next: 010.6A API-to-PostgreSQL posting verification using existing Testcontainers conventions.
- 010.5D pushed as `4593853`.
- 010.6A: API posting verified end-to-end with PostgreSQL 18 Testcontainers, migrations, seeded accounts, independent read scope, normalized currencies and exact debit/credit values. Focused test and all 138 tests passed; build clean. No production changes needed.
- Next: complete HTTP invariant rejection coverage before transaction retrieval.

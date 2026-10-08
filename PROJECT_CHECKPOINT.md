# SettleCore checkpoint — 2026-09-27

## Current position
- Active repository: `/Users/suthankan/Desktop/Projects/SettleCore`, branch `main`.
- 011.3C committed/pushed as `6f247da`: Ledger handler registration and Payments-owned port adapter DI.
- 011.4A committed/pushed as `baafb06`: exact positive decimal-to-long minor units with explicit precision; no rounding.
- 011.4B already committed/pushed as `77b3221` when this session started: Payments currency policy supports SGD=2, JPY=0, KWD=3, case normalization, no fallback.
- 011.4C committed/pushed as `6413d63`: `PaymentAmountConversion.ToMinorUnits(decimal, string)` composes the currency policy and exact converter. 16 new tests cover precision, lowercase, fractional units, invalid currency, null, nonpositive and overflow amounts.

- 011.5A committed/pushed as `957787b`: Ledger repository treats matching transaction IDs/ledger/entry multisets as idempotent; different contents throw LedgerTransactionConflictException. PostgreSQL unique-key races reload the winner and detach the failed insert. No schema change.

- 011.5B (this checkpoint-containing commit): ledger HTTP endpoint returns 409 on conflicting transaction identity; identical retries retain the existing 201/location response without duplicate writes.

## Verification
- 011.4B baseline: 205 solution tests passed.
- 011.4C: compile-time RED confirmed; then 221 solution tests passed, zero failures/skips, including PostgreSQL Testcontainers. Full solution build: zero warnings/errors.

- 011.5A: four PostgreSQL RED tests then GREEN, including deterministic concurrent writers, reversed entry order, same-context repeats, conflicts and context reuse. Full solution: 225 passed, no failures/skips; build zero warnings/errors.

- 011.5B: PostgreSQL HTTP test RED (500 instead of 409), then full solution 226 passed, zero failures/skips. Build zero warnings/errors; diff check clean.

## Architecture and next slices
- Preserve .NET 10/C#14 modular monolith, Minimal APIs, module-owned DI and EF/PostgreSQL. Backend and tests before frontend/deployment.
- Payments owns IPaymentLedgerPostingPort and PaymentLedgerPostingRequest; Payments Infrastructure adapts to Ledger PostPaymentLedgerTransactionHandler.
- Payment.Amount remains decimal; Ledger uses integer minor units. Currency-aware conversion is available but success integration remains unwired.
- MarkPaymentSucceededCommand carries only PaymentId; handler marks/saves status only. Accounting routing and authoritative fee source are not defined. Do not invent either.
- USER DECISION CONFIRMED 2026-09-27: require explicit posting inputs from the caller (ledger/account routing and exact fee); do not invent merchant configuration or fee formulas.
- Next intended slice 011.6A: model explicit payment-success posting inputs and build a Payments-owned posting request from stored Payment.Amount/Currency using the currency-aware converter. Include stable caller-supplied TransactionId, LedgerId, the three account IDs and an explicitly unit-labelled fee. Validate without writing or changing payment status. Follow existing Ledger fee constraints; any change to zero-fee support is separate scope.
- Then add durable Payments-owned posting intent, uniquely keyed by payment, storing the complete immutable payload and stable transaction ID atomically with status in the Payments database. Add PostgreSQL rollback and concurrent retry/conflict tests before changing the success endpoint. Existing status-only handler must not call Ledger directly.
- After atomic success/intent persistence is verified, add dispatch/retry/completion semantics through IPaymentLedgerPostingPort. Ledger idempotency now handles matching/conflicting concurrent identities; protect against differing payloads for the same payment even when callers supply different transaction IDs.
- Repository retry protection is tested for current standalone SaveChanges usage. If wrapping Ledger writes in an explicit transaction later, verify isolation/savepoint recovery separately.
- Define durable posting intent together with success persistence before connecting stores; no independent status/posting writes.
- Remaining backend: payment-ledger integration, audit, provider ingestion, webhooks, workers/outbox/idempotency, observability. No frontend work performed.

## Commit and continuity
- User authorizes commit/push after each logical green slice to `https://github.com/Suthankan1/SettleCore.git`.
- Remote main pushed at `957787b` before 011.5B. This commit contains 011.5B and checkpoint; use git log for final hash.
- Current handback threshold: around 10% remaining, superseding 4%. Last observed: 14% short-window and 87% weekly remaining.
- Original chat: `6ab2b1a8-08b4-83ee-b497-fa1440530122` (C# .NET Project Idea).

## Handoff
- Autonomous work stopping at the verified 011.5B boundary near the capacity threshold; no uncommitted implementation work intended. Verify local HEAD/remote match after push.
- Completed this run: 011.4C `6413d63`, 011.5A `957787b`, and 011.5B (checkpoint-containing commit). 011.4B `77b3221` was already present and pushed.
- No payment-success-to-ledger wiring, outbox worker, provider ingestion, audit or frontend implementation added in this run.

## 2026-10-08 — 011.6D
- Baseline 011.6C: `873825f`, current source checkout clean at start.
- Working repository for this run: local project directory `SettleCore-current` (cloned from `/Users/suthankan/Desktop/Projects/SettleCore`). Original source checkout remains at the baseline.
- Added Payments-owned IPaymentLedgerPostingIntentRepository and minimal EF AddAsync/GetByPaymentIdAsync implementation, following existing repository conventions.
- Compile-time RED captured for missing repository types before implementation. PostgreSQL GREEN reloads the complete immutable payload through an independent context and checks missing payment returns null.
- Focused test 1 passed; Payments infrastructure 12 passed; full solution 239 passed, zero failed/skipped; full build zero warnings/errors.
- Docker Desktop was stopped; started successfully to enable Testcontainers.
- Next small step: repository DI registration with focused resolution test, then explicit atomic payment-success/intent boundary and PostgreSQL rollback tests. Standalone AddAsync follows existing SaveChanges conventions; atomic integration must use a shared transaction/boundary rather than independent commits.
- Posting inputs stay explicit; no accounting routing or fee policy invented. No frontend changes.
- This checkpoint is included in the 011.6D commit; verify final hash and push status with Git.

## 2026-10-08 — repository registration
- 011.6D committed and pushed: `35540b5`.
- Added scoped intent repository registration to AddPaymentsModule. Focused host integration test captured missing-service RED, then GREEN; verifies implementation type and same/different scope lifetime.
- Full solution 240 passed, zero failed/skipped. Build zero warnings/errors.
- Next: atomic payment-success + posting-intent persistence boundary, starting with PostgreSQL happy-path and rollback RED tests; then retry/conflict coverage before endpoint changes.
- Latest quota: 89% short-window and 92% weekly remaining; handback threshold not reached.

## 2026-10-08 — 011.6E atomic persistence foundation
- Repository registration committed/pushed as `3587e31`.
- Added IPaymentSuccessPersistence and EF implementation saving succeeded payment and pending intent in one SaveChanges transaction; no Ledger call and no endpoint change.
- Compile-time RED captured before implementation. Two PostgreSQL GREEN cases verify happy path and rollback of status when intent insertion fails with duplicate key.
- Payments infrastructure 14 passed; full solution 242 passed, zero failed/skipped; full build zero warnings/errors.
- Next: guard the boundary against mismatched payment/intent identity and invalid status, then matching/conflicting concurrent retries with database-backed tests before application/API wiring.
- This foundation deliberately does not yet provide retry semantics or DI registration. Keep old status-only flow disconnected from Ledger.

## 2026-10-08 — atomic boundary guards
- Atomic persistence foundation committed/pushed: `2288914`.
- Two PostgreSQL assertion RED cases demonstrated mismatched identities and pending status were accepted. Minimal guards now reject both before writing; independent read proves unchanged pending payment and no intent.
- Focused atomic tests 4 passed; Payments infrastructure 16 passed; full build zero warnings/errors. Last full solution: 242 passed before guards.
- Next: matching/conflicting retry semantics for atomic persistence, then deterministic concurrent database writers; do not wire endpoint until these pass.

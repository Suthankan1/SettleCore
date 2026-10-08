# SettleCore checkpoint — 2026-10-08

## Current handback
- Resumed from 011.6C, `873825f`. Completed 011.6D repository boundary through 011.8D opt-in automatic posting worker, in separately tested, committed and pushed slices.
- Working copy used: `/Users/suthankan/.codex/.chatgpt-projects/g-p-6a87d9fd8d408191ab054e1d87dc13d6/SettleCore-current`, branch `main`, remote `https://github.com/Suthankan1/SettleCore.git`.
- Primary repository: `/Users/suthankan/Desktop/Projects/SettleCore`; clean baseline verified before final synchronization. Fast-forward it to the final pushed commit; do not reset or overwrite local changes.
- Final production verification: focused automatic-worker PostgreSQL test passed; all 69 host integration tests passed; full solution 275 passed, zero failed/skipped; full build zero warnings/errors.
- Last prior commit: `7a4c5cf` (failure-isolated batch processor), pushed. This checkpoint is included in the hosted-worker commit; obtain its exact hash with `git log -1 --oneline`. No unfinished implementation is intended at handback.
- Quota threshold: stop new slices around 10% remaining. Last check during closing validation: 13% short-window, 80% weekly remaining. Do not consume reset credits automatically.

## Current behavior and operating requirements
- Success endpoint requires caller-supplied transaction/ledger/account IDs and fee explicitly labelled in minor units. Factory uses stored payment amount/currency; no fee formula or accounting routing defaults.
- Succeeded status and complete immutable pending intent persist atomically. Matching retries are idempotent; changed payloads produce 409, including changed transaction IDs for the same payment. Concurrent writers and true PostgreSQL rollback verified.
- Dispatch calls Ledger before acknowledging Posted. Lost acknowledgment replays the stable transaction identity and exact payload; real separate-store PostgreSQL tests prove one balanced transaction/three entries.
- Pending batches are bounded, require committed payment success, exclude posted/orphan/inconsistent records, and filter persisted next-attempt times. Each intent has its own scope. Failures log and schedule delayed retry; host shutdown cancellation leaves durable pending work.
- `PaymentLedgerPostingWorker` is disabled by default. Apply Payments and Ledger migrations and provision the caller's ledger accounts before enabling it. Set `PaymentLedgerPostingWorker:Enabled=true` (environment variable `PaymentLedgerPostingWorker__Enabled=true`). Operational defaults: BatchSize=100, PollIntervalMilliseconds=5000, RetryDelaySeconds=60; all must be positive and are validated at startup. Test enables the worker explicitly and verifies HTTP → automatic dispatch → Ledger.
- New migration: `20261008113952_AddPaymentLedgerPostingRetrySchedule`, adding only nullable intent `next_attempt_at` and the status/next-attempt/payment index.
- No frontend work. Audit trail, provider ingestion/webhooks, broader observability, deployment and other remaining backend roadmap work are not complete.

## Next intended small slice
- 011.8E: read-only posting-intent status lookup by payment ID, starting with RED application query tests, then minimal result/handler, DI and PostgreSQL HTTP tests. Expose pending/posted status and next-attempt time so delayed/failed dispatch can be inspected.
- Then strengthen batch scheduling-failure isolation and hosted-worker failure/restart coverage, add posting metrics/audit, and continue defined backend work. Provider-specific behavior requires actual provider requirements; do not invent signatures or event contracts.
- Preserve focused RED → minimal GREEN → relevant module checks → full solution/build at meaningful integration boundaries → separate commit/push, with a checkpoint per slice.

## 011.8D hosted worker closing slice
- PostgreSQL assertion RED captured: HTTP success persisted intent but it remained Pending without a worker. GREEN with enabled hosted worker completes it automatically and retains one balanced Ledger transaction with three entries.
- Added validated opt-in worker options, cancellation-aware polling, batch failure logging/retry, TimeProvider/batch DI and default disabled configuration.
- Host factory explicitly replaces both database contexts so the end-to-end test uses its separate isolated stores.

## Historical checkpoint — 2026-09-27

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

## 2026-10-08 — sequential posting intent retries
- Boundary guards committed/pushed: `f55422a`.
- Added Payments-owned PaymentLedgerPostingIntentConflictException. Atomic boundary returns successfully for identical immutable posting payload; conflicts compare transaction ID, ledger/account IDs, currency, gross and fee independently.
- Compile-time RED captured for missing conflict type. Nine PostgreSQL GREEN cases cover same-context and fresh-context identical retries plus all eight changed payload fields; one original intent retained.
- Rollback test now forces a database length constraint failure, preserving true failed-write rollback coverage after duplicate handling changed.
- Focused atomic/retry tests 13 passed; Payments infrastructure 25 passed; full solution 253 passed, zero failed/skipped; full build zero warnings/errors.
- Next: deterministic simultaneous writers, matching and conflicting payloads; catch only the intent primary-key race, reload committed winner and clear failed tracked writes so context can be reused. No endpoint or dispatch wiring yet.

## 2026-10-08 — concurrent posting intent retries
- Sequential retry protection committed/pushed: `eaf5c2a`.
- Two deterministic PostgreSQL RED cases forced both writers past missing-intent lookup before SaveChanges; observed raw duplicate key failures.
- GREEN catches only PK_payment_ledger_posting_intents unique violation, detaches the failed payment/intent pair, reloads committed winner, then compares full immutable payload. Unrelated tracking is retained.
- Matching writers both succeed; conflicting writers have exactly one success and one Payments conflict. Both contexts can subsequently SaveChanges without replaying failed writes.
- Focused race cases 2 passed; Payments infrastructure 27 passed; full solution 255 passed, zero failed/skipped; full build zero warnings/errors.
- Next: application flow accepting explicit posting input, factory validation before status mutation, and one IPaymentSuccessPersistence call. Then DI and PostgreSQL endpoint tests; dispatch/completion later. Old status-only endpoint remains unchanged until that slice.
- SaveChanges implicit transactions verified; outer explicit transaction/savepoint usage is not covered and should not be introduced without separate tests.

## 2026-10-08 — explicit-input application flow
- Concurrent retry recovery committed/pushed: `2da1630`.
- Added RecordPaymentSuccessCommand/Handler accepting caller-supplied PaymentLedgerPostingInput. Uses stored amount/currency and established request factory; validates before status mutation; saves payment and full pending intent through one atomic boundary call.
- Succeeded retries avoid calling domain MarkSucceeded again and are checked by persistence. Missing payment returns null; invalid fee leaves pending status and makes no persistence call.
- Compile-time RED captured; four focused GREEN cases, all 101 Payments unit tests passed; solution build zero warnings/errors. Last full solution 255 passed before this new handler.
- Application flow is not yet registered or exposed; old status-only handler/endpoint unchanged. Next: verify persisted-state consistency for pre-existing intent retries, then register application flow and require explicit posting body at success endpoint with PostgreSQL API RED tests.
- Quota last observed: 71% short-window, 89% weekly remaining.

## 2026-10-08 — persisted retry consistency
- Explicit-input application handler committed/pushed: `543f467`.
- PostgreSQL RED exposed false successful acknowledgment when identical intent existed but stored payment remained pending. Boundary now checks actual persisted payment success before acknowledging retries, and detaches unsaved status on pre-existing intent paths.
- GREEN confirms inconsistent pair is rejected and later SaveChanges cannot leak the attempted success. Focused 1 passed; Payments infrastructure 28 passed; solution build zero warnings/errors. Last full solution 255 passed before application and this guard.
- Next: API body/DI wiring, with PostgreSQL success+intent round-trip, matching retry, changed-payload 409, missing body/invalid input 400 and missing payment 404 tests before dispatch work.

## 2026-10-08 — 011.6F explicit-input success endpoint
- Persisted-state consistency committed/pushed: `27186e1`.
- POST /payments/{id}/succeed now requires PaymentLedgerPostingInput JSON body and resolves RecordPaymentSuccessHandler/IPaymentSuccessPersistence through scoped module DI.
- PostgreSQL API assertion RED: missing body previously returned 200 and mutated status. GREEN validates missing/invalid body 400 without writes, missing payment 404, valid success and identical retry 200, changed transaction ID 409; independently reloads exact payload, one pending intent and succeeded payment.
- Existing HTTP tests now send explicit inputs; already-succeeded explicit retry is allowed. Old status-only application handler remains available but endpoint does not use it or directly call Ledger.
- Focused API suite 9 passed; host integration 63 passed; full solution 261 passed, zero failed/skipped; build zero warnings/errors.
- Next: map amount conversion overflow to HTTP validation failure without mutation; then durable intent completion/dispatch semantics and retry after Ledger success but before acknowledgment. No accounting or fee defaults introduced.

## 2026-10-08 — amount overflow regression
- Explicit-input success endpoint committed/pushed: `ae06de7`.
- Added HTTP regression test for decimal.MaxValue conversion: already GREEN through existing converter validation. No production change required; pending status and no persistence call verified.
- All four focused success endpoint tests pass. Last full solution 261 passed; build clean at previous production slice.
- Next: Pending → Posted intent completion lifecycle, durable acknowledgment and application dispatch via existing Payments-owned port; prove failed posting stays pending and replay after Ledger success is idempotent.

## 2026-10-08 — 011.7A posting completion lifecycle
- Amount overflow regression committed/pushed: `37d0de0`.
- Added Posted status and idempotent MarkPosted transition; all posting identity/account/currency/amount fields remain immutable.
- Compile-time RED captured before adding lifecycle method/state; focused 2 passed, Payments unit 102 passed; solution build zero warnings/errors. No migration required: status already persisted as unconstrained string.
- Next: database-backed guarded acknowledgment (pending → posted, repeated acknowledgment safe, missing intent no write), then dispatch via IPaymentLedgerPostingPort and restart/replay tests.

## 2026-10-08 — 011.7B durable acknowledgment
- Completion lifecycle committed/pushed: `6ed1833`.
- Added repository MarkPostedAsync: guarded status-only database update, idempotent acknowledgment returns true, missing intent returns false. Reads use AsNoTracking so acknowledgment cannot be hidden by previously tracked pending objects.
- Compile-time RED captured before repository API. PostgreSQL GREEN verifies repeated/missing acknowledgment, fresh same-context reads, independent reload, all immutable payload fields preserved, later SaveChanges cannot revert completion.
- Payments infrastructure 29 passed; full solution 264 passed, zero failed/skipped; build zero warnings/errors.
- Next: focused application dispatch RED tests for port success/failure, durable acknowledgment failure then retry with the same transaction ID, and missing/already-posted no-op behavior. Then real Ledger/PostgreSQL replay integration and pending worker queries.

## 2026-10-08 — 011.7C dispatch application semantics
- Durable acknowledgment committed/pushed: `e3fb39e`.
- Added DispatchPaymentLedgerPostingCommand/Handler. Missing intent returns false, posted intent returns true without port call; pending intent posts exact immutable payload, then durably acknowledges.
- Port/acknowledgment failures propagate and leave pending intent for replay using unchanged transaction identity; no cross-module transaction introduced.
- Compile-time RED captured. Four focused GREEN cases cover success/order/completed skip, both failure boundaries with retry, and missing intent. Payments unit 106 passed; solution build zero warnings/errors. Last full solution 264 passed before dispatch.
- Dispatch not registered yet. Next: real composition/PostgreSQL test with separate Payments and Ledger stores, simulate failure after Ledger commit but before Payments acknowledgment, restart scope and prove one transaction/three entries plus posted intent. Then worker pending query and dispatch loop.

## 2026-10-08 — 011.7D real cross-store replay
- Dispatch application semantics committed/pushed: `080cb48`.
- Registered scoped dispatch handler. PostgreSQL composition RED captured missing registration; GREEN exercises actual Payments repository, Ledger adapter/handler/repository and separate database stores.
- Happy dispatch and failure after Ledger commit/before Payments acknowledgment both verified. Fresh-scope replay finishes pending intent; retains one transaction and exactly three correct balanced entries, stable transaction/ledger identities.
- Focused 2 passed; host integration 66 passed; full solution 270 passed, zero failed/skipped; full build zero warnings/errors.
- Next: bounded pending query for worker, followed by failure isolation/retry scheduling and hosted dispatch. Avoid starvation from permanently failing intents; no fee/account routing defaults.
- Latest quota observed: 44% short-window and 85% weekly remaining; stop new changes around 10% remaining.

## 2026-10-08 — 011.8A pending worker batches
- Cross-store replay composition committed/pushed: `5d7956a`.
- Added bounded ordered GetPendingAsync repository query, selecting pending intents only for committed succeeded payments. Posted intents, pending payments and orphan intents excluded; nonpositive limit rejected.
- Compile-time RED captured; PostgreSQL GREEN verifies bounds/order and all exclusions. Payments infrastructure 30 passed; solution build zero warnings/errors. Last full solution 270 passed before query.
- Next: durable next-attempt scheduling with due filtering and an index, tested before hosted worker. Failed records must not monopolize the bounded queue. Operational retry delay remains configurable; accounting inputs stay explicit.

## 2026-10-08 — 011.8B durable retry schedule
- Pending worker query committed/pushed: `4ff8988`.
- Added nullable UTC NextAttemptAt, guarded ScheduleRetryAsync, due-time filtering with injectable TimeProvider, and status/next-attempt/payment index. Migration adds only the intent timestamp column and index; snapshot updated. Generated index array adjusted to satisfy strict analyzer.
- Compile-time RED captured. PostgreSQL GREEN verifies persisted schedule, exact due-time inclusion, fresh-context filtering, missing/posted scheduling false and no revival of completed intents.
- Payments infrastructure 31 passed; full solution 272 passed, zero failed/skipped; solution build zero warnings/errors.
- Next: bounded batch processor with fresh scopes per intent, configurable retry delay, failure isolation/logging and cancellation propagation. Then opt-in hosted worker configuration and full HTTP→worker→Ledger verification.

## 2026-10-08 — 011.8C failure-isolated batch processor
- Durable retry scheduling committed/pushed: `1bdf407`.
- Added PaymentLedgerPostingBatchProcessor: bounded selection scope, fresh async scope per intent, completed count, structured failure logging and persisted next-attempt scheduling. Retry delay is supplied explicitly; cancellation propagates without retry.
- Compile-time RED captured. PostgreSQL GREEN proves failing first intent does not block successful second, failed intent not reattempted before due, due retry completes, cancellation leaves all pending and no scheduling writes.
- Focused 2 passed; host integration 68 passed; full solution build zero warnings/errors. Last full solution 272 passed before batch processor.
- Next: opt-in hosted worker with validated operational settings, default disabled until migrations/connections/accounts are configured; HTTP→automatic dispatch→separate Ledger DB test. Then checkpoint near 10% quota.
- Remaining backend beyond payment dispatch: audit, provider ingestion/webhooks, additional idempotency, observability and deployment still not completed. No frontend work.

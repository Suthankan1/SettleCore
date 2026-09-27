# SettleCore checkpoint — 2026-09-27

## Current position
- Active repository: `/Users/suthankan/Desktop/Projects/SettleCore`, branch `main`.
- 011.3C committed/pushed as `6f247da`: Ledger handler registration and Payments-owned port adapter DI.
- 011.4A committed/pushed as `baafb06`: exact positive decimal-to-long minor units with explicit precision; no rounding.
- 011.4B already committed/pushed as `77b3221` when this session started: Payments currency policy supports SGD=2, JPY=0, KWD=3, case normalization, no fallback.
- 011.4C (this checkpoint-containing commit): `PaymentAmountConversion.ToMinorUnits(decimal, string)` composes the currency policy and exact converter. 16 new tests cover precision, lowercase, fractional units, invalid currency, null, nonpositive and overflow amounts.

## Verification
- 011.4B baseline: 205 solution tests passed.
- 011.4C: compile-time RED confirmed; then 221 solution tests passed, zero failures/skips, including PostgreSQL Testcontainers. Full solution build: zero warnings/errors.

## Architecture and next slices
- Preserve .NET 10/C#14 modular monolith, Minimal APIs, module-owned DI and EF/PostgreSQL. Backend and tests before frontend/deployment.
- Payments owns IPaymentLedgerPostingPort and PaymentLedgerPostingRequest; Payments Infrastructure adapts to Ledger PostPaymentLedgerTransactionHandler.
- Payment.Amount remains decimal; Ledger uses integer minor units. Currency-aware conversion is available but success integration remains unwired.
- MarkPaymentSucceededCommand carries only PaymentId; handler marks/saves status only. Accounting routing and authoritative fee source are not defined. Do not invent either.
- Next safe technical slice: ledger duplicate/retry protection using stable caller-provided transaction identity and payload conflict detection. Concurrent retry safety must be tested against PostgreSQL before any outbox integration.
- Define durable posting intent together with success persistence before connecting stores; no independent status/posting writes.
- Remaining backend: payment-ledger integration, audit, provider ingestion, webhooks, workers/outbox/idempotency, observability. No frontend work performed.

## Commit and continuity
- User authorizes commit/push after each logical green slice to `https://github.com/Suthankan1/SettleCore.git`.
- Remote main verified at `77b3221` before 011.4C. This commit contains 011.4C and its checkpoint; use git log for its final hash.
- Current handback threshold: around 10% remaining, superseding 4%. Last observed: 96% short-window and 99% weekly remaining.
- Original chat: `6ab2b1a8-08b4-83ee-b497-fa1440530122` (C# .NET Project Idea).

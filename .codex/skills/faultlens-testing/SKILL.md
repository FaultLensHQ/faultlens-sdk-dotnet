# FaultLens Test & Validation Skill

## Purpose

Design and review deterministic FaultLens tests that prove customer-visible and durable application behavior through production application boundaries while substituting external systems safely.

Use this skill for unit, application, API, integration, CommitIntegration, lifecycle, provider, billing, concurrency, recovery, and regression testing. Apply the Backend and Database skills as well when their contracts are material.

## Core testing standard

A test must prove the behavior that production relies on, not manufacture a passing database state.

Prefer the highest realistic FaultLens boundary that remains deterministic: drive business state through public/application services and APIs, persist through the real repositories and PostgreSQL path where persistence matters, and substitute only boundaries that are genuinely external to FaultLens.

## Deterministic time

- Application code must use `IClock` (or the repository's approved clock abstraction) for current-time access. Direct `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, or equivalent wall-clock reads are not allowed in application/domain/service/worker business logic.
- This rule applies even when the current code appears simple or is not yet covered by a time-sensitive test. Time is an injected dependency, not a global runtime dependency.
- If implementation or test work exposes an existing direct wall-clock read that affects the behavior under test, fix the application path to use `IClock`; do not work around it in the test, sleep until the desired time, loosen assertions, or manufacture database state. Keep the production correction bounded to the affected path and add/adjust deterministic coverage.
- Infrastructure/framework code may use a platform clock only where it is genuinely outside business/application time semantics; exceptions must be explicit and justified in review.
- Tests control `IClock` deterministically. In .NET tests, use NSubstitute for `IClock` unless an established repository deterministic clock fixture is more appropriate.
- Never wait for real time, change machine time, depend on wall-clock execution, use `Task.Delay` to cross a business boundary, or introduce direct `DateTime.UtcNow` / `DateTime.Now` into testable business logic.
- Move the mocked clock explicitly to prove just-before, exact-boundary, and just-after behavior.
- Time-sensitive billing coverage must include applicable month-end and short-month behavior, leap years, annual anniversaries, period start/end semantics, expiry, grace, retries, reconciliation windows, and timezone/calendar rules defined by the product contract.
- Tests must assert authoritative persisted timestamps/periods where those values affect future behavior; checking only returned DTOs is insufficient.

## External provider boundary

Third-party systems are substituted in automated application/lifecycle tests. Do not make live Razorpay, SES, ZeptoMail, GitHub, Jira, identity-provider, or other external calls merely to prove FaultLens behavior.

- Mock/fake the provider at the narrow FaultLens provider abstraction, not by bypassing FaultLens application orchestration.
- Model realistic provider responses and identities.
- Cover applicable success, rejection/decline, timeout, malformed or incomplete response, duplicate delivery, delayed completion, uncertain outcome, retry, reconciliation, revocation/expiry, and provider readback.
- Provider callbacks/browser redirects/webhooks are correlation evidence only when the production contract says so; tests must prove the authoritative server-side verification path.
- Keep provider-specific contract tests separate from FaultLens lifecycle tests when a real sandbox/provider boundary is explicitly required.

The goal is application end-to-end proof through FaultLens and real persistence while only genuine external systems are substituted.

## Test-state mutation boundary

Business state must be created and changed through existing production application services, APIs, commands, workers, or other approved production workflow boundaries.

Tests must not use ad-hoc `INSERT`, `UPDATE`, `DELETE`, SQL scripts, direct table mutation, or repository shortcuts merely to manufacture a lifecycle state that production reaches through application behavior.

Read-only SQL/database inspection is allowed when needed to assert durable truth that is not exposed safely elsewhere.

Direct SQL mutation in a test is an exceptional escape hatch. It is allowed only when:

1. the required state cannot reasonably be produced through an existing production application path;
2. the mutation is necessary to prove a database/migration/corruption/recovery condition rather than to make normal setup easier;
3. the reason and exact mutation are documented in the test/PR;
4. explicit owner/ChatGPT approval is obtained before introducing it; and
5. the test still proves the production recovery/behavior path after the exceptional setup.

Migration SQL and DbMigrator execution are not test-data shortcuts and remain governed by the Database skill.

Never weaken or add production application APIs solely to make test setup convenient.

## Durable lifecycle acceptance

When a story changes a durable lifecycle, the acceptance test should cross the complete material lifecycle rather than prove isolated methods independently.

Where applicable, prove:

- authoritative creation through application services;
- persisted state after commit;
- cross-connection visibility;
- explicit time advancement through `IClock`;
- worker/obligation generation;
- substituted external provider interaction and provider readback;
- settlement/convergence through production services;
- final persisted business state and customer-visible projection;
- replay/idempotency;
- retry/reconciliation after uncertain outcomes;
- concurrency/fencing when competing execution is a production risk.

For billing, payment, entitlement, retention, severity, notification, and similar workflows, assert the durable facts that authorize the next lifecycle step, not only mocked calls.

## Test-double discipline

Mock external dependencies and controllable nondeterminism. Do not mock away the FaultLens behavior being accepted.

- Pure domain tests may mock direct collaborators narrowly.
- Application orchestration tests may substitute external/provider boundaries while retaining real orchestration.
- Persistence acceptance uses the real supported PostgreSQL path when correctness depends on PostgreSQL.
- Do not replace repositories/database semantics with mocks when the acceptance criterion is persistence, transaction, locking, uniqueness, or cross-connection behavior.
- Verify meaningful outputs and durable state; interaction assertions alone are not acceptance.

## Failure and edge-case matrix

Derive cases from the actual lifecycle invariants. At minimum consider:

- boundary timestamps and expiry;
- duplicate/replayed requests;
- retry after known failure;
- uncertain provider outcome followed by reconciliation;
- stale/late provider evidence;
- tenant/identity mismatch;
- amount/currency/identity mismatch for commercial workflows;
- concurrent claims/writers where applicable;
- process restart/re-entry using durable state;
- unsupported state/rail/configuration fails closed.

Do not create a huge combinatorial suite without product value. Cover distinct semantic boundaries.

## Review gate

A test change is not sufficient merely because CI is green. Independent review must ask:

- Did tests reach state through production application behavior?
- Does the application path obtain current business time through `IClock` rather than direct wall-clock APIs?
- If a test failure exposed direct `DateTime`/`DateTimeOffset` current-time access, was the application corrected to use `IClock` instead of weakening or working around the test?
- Is business time deterministic through `IClock`?
- Are external systems substituted at the correct boundary?
- Does persistence-sensitive behavior use real PostgreSQL?
- Are durable state and customer-visible outcomes asserted?
- Are replay/retry/reconciliation/concurrency cases covered where required?
- Did any direct SQL mutation bypass production semantics, and if so is the explicit approval and justification recorded?
- Does the test prove the complete acceptance lifecycle requested by the story?

Report material missing evidence as a validation gap rather than inferring correctness from unit coverage.

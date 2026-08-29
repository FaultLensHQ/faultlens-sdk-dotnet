---
name: faultlens-engineering
description: Route non-trivial FaultLens .NET SDK work through Product Decisions, Design, implementation, and strict Review with governed compute/context economy.
---

# FaultLens Engineering Router — .NET SDK

This is a router, not a source of public API/product truth.

## Operating-role boundary

Canonical role governance comes from `FaultLensHQ/faultlens-engineering/docs/agents/operating-roles.md`, governance **1.2.7**, released source commit `c43eb36d81d157858c0ea03c0c355007f45320e5`.

- **AI Governor** applies to agent/skill/governance/rule/version/distribution changes.
- **Architecture & Product Governor** applies when an execution agent returns implementation for acceptance. Independently inspect the actual current PR/head/base/diff/evidence rather than trusting the completion report.
- **Execution Agent** applies to implementation, remediation, testing and repository execution. It does not approve its own implementation for merge.
- Implementation completion/checkpoint/`READY FOR INDEPENDENT RE-REVIEW` means review first. If blocked, produce a bounded remediation prompt. If review passes, require normal merge gates and explicit merge authorization; after merge, verify the default branch before generating the next-story prompt.
- Tool availability does not redefine the active role. Do not silently cross from governor/reviewer into repository-local execution.

Use the smallest applicable path:

1. establish current public API/runtime behavior and customer integration impact;
2. `faultlens-product-decisions` for product semantics, capture/privacy policy, public API compatibility, priority or non-goals;
3. `faultlens-design` for implementation-ready public contract/migration/runtime design;
4. implementation and repository validation;
5. strict independent review.

## Compute and context economy

Canonical compute/context governance comes from `FaultLensHQ/faultlens-engineering/docs/ai-compute-context-economy.md`, governance **1.2.7**, released source commit `c43eb36d81d157858c0ea03c0c355007f45320e5`.

- Use the strongest reasoning tier for Product Decisions, Design, unresolved public-contract/privacy/compatibility/migration/data-correctness reasoning, difficult debugging whose invariant is not yet understood, and strict independent Review.
- Once the accepted contract is implementation-ready, default normal implementation, refactoring, repository investigation, ordinary remediation and test authoring to a **medium-cost capable execution model**. Do not interpret `strongest practical` as `strongest available`.
- Prefer the **lowest-cost capable mechanical tier** for deterministic/repetitive work such as test/build/pack re-runs, formatting/lint remediation, straightforward tests against an explicit contract, repetitive edits, documentation, CI/log/status inspection and already-specified remediation.
- Do not retain or escalate to the reasoning tier merely because the task originated there. A lower tier that encounters ambiguity, contradiction or material risk must stop/escalate rather than guess.
- At coherent work boundaries, checkpoint mutable state in GitHub/accepted durable handoffs and prefer a fresh execution context when prior conversation state is mostly completed work.
- Load the smallest authoritative slices required for the active task; do not carry large logs, completed investigations, unchanged code, repeated validation output, superseded reasoning or complete issue/PR histories merely for continuity.
- Compute/context economy must never weaken public API compatibility, privacy, host-application safety, correctness, validation evidence or independent review.
- Use capability-based routing; do not hard-code provider/model version names into durable governance.

Unexpected evidence may reopen Product Decisions/Design; implementation convenience may not authorize a breaking public-contract change.

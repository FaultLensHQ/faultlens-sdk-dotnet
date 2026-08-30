---
name: faultlens-engineering
description: Route non-trivial FaultLens .NET SDK work through Product Decisions, Design, implementation, and strict Review with governed compute/context economy.
---

# FaultLens Engineering Router — .NET SDK

This is a router, not a source of public API/product truth.

## Operating-role boundary

Canonical role governance comes from `FaultLensHQ/faultlens-engineering/docs/agents/operating-roles.md`, governance **1.2.10**, released source commit `30b843fc52b127db1a1acf3ee389e6227391a721`.

- **AI Governor** applies to agent/skill/governance/rule/version/distribution changes.
- **Architecture & Product Governor** applies when an execution agent returns implementation for acceptance. Independently inspect the actual current PR/head/base/diff/evidence rather than trusting the completion report.
- **Execution Agent** applies to implementation, remediation, testing and repository execution. It does not approve its own implementation for merge.
- Implementation completion/checkpoint/`READY FOR INDEPENDENT REVIEW`/`READY FOR INDEPENDENT RE-REVIEW` means review first. If blocked, record the PR as blocked/not merge-ready and produce a bounded remediation prompt. If review passes, require normal current-base/exact-head merge gates and explicit merge authorization; after merge, verify the default branch before generating the next-story prompt.
- Tool availability does not redefine the active role. Do not silently cross from governor/reviewer into repository-local execution.

Use the smallest applicable path:

1. establish current public API/runtime behavior and customer integration impact;
2. `faultlens-product-decisions` for product semantics, capture/privacy policy, public API compatibility, priority or non-goals;
3. `faultlens-design` for implementation-ready public contract/migration/runtime design;
4. implementation and focused local validation;
5. strict independent review.

## Compute and context economy

Canonical compute/context governance comes from `FaultLensHQ/faultlens-engineering/docs/ai-compute-context-economy.md`, governance **1.2.10**, released source commit `30b843fc52b127db1a1acf3ee389e6227391a721`.

- Use the strongest reasoning tier for Product Decisions, Design, unresolved public-contract/privacy/compatibility/migration/data-correctness reasoning, difficult debugging whose invariant is not yet understood, and strict independent Review.
- Once the accepted contract is implementation-ready, default normal implementation, refactoring, repository investigation, ordinary remediation and test authoring to a **medium-cost capable execution model**. Do not interpret `strongest practical` as `strongest available`.
- Prefer the **lowest-cost capable mechanical tier** for deterministic/repetitive work such as test/build/pack re-runs, formatting/lint remediation, straightforward tests against an explicit contract, repetitive edits, documentation, CI/log/status inspection and already-specified remediation.
- Do not retain or escalate to the reasoning tier merely because the task originated there. A lower tier that encounters ambiguity, contradiction or material risk must stop/escalate rather than guess.
- At coherent work boundaries, checkpoint mutable state in GitHub/accepted durable handoffs and prefer a fresh execution context when prior conversation state is mostly completed work.
- Load the smallest authoritative slices required for the active task; do not carry large logs, completed investigations, unchanged code, repeated test output, superseded reasoning or complete issue/PR histories merely for continuity.
- Compute/context economy must never weaken public API compatibility, privacy, host-application safety, correctness, validation evidence or independent review.
- Use capability-based routing; do not hard-code provider/model version names into durable governance.

## Workspace and repository resolution

Canonical local-workspace governance comes from `FaultLensHQ/faultlens-engineering/docs/engineering/local-workspace-resolution.md`, governance **1.2.10**, released source commit `30b843fc52b127db1a1acf3ee389e6227391a721`.

Before cloning or reconstructing a FaultLens repository, resolve `FaultLensHQ/<repo>` against the configured local workspace/repository map, verify the candidate Git working tree and remote identity, and reuse the valid local checkout. GitHub remains authoritative for durable issues, PRs, review state, hosted CI and remote evidence. Local-first execution does not authorize stale-ref assumptions or destructive worktree cleanup.

## Pull-request validation and review state

Canonical PR validation/review governance comes from `FaultLensHQ/faultlens-engineering/docs/engineering/pr-validation-and-review-state.md`, governance **1.2.10**, released source commit `30b843fc52b127db1a1acf3ee389e6227391a721`.

- Open implementation PRs as non-Draft / Ready for review by default. GitHub Ready state is not FaultLens merge approval.
- While implementation is incomplete, record `IMPLEMENTATION IN PROGRESS — NOT READY FOR GOVERNOR REVIEW` in durable PR evidence.
- When the final pushed head and required focused local evidence are ready, record `READY FOR INDEPENDENT REVIEW` and hand off.
- Never self-declare `APPROVED / MERGE-READY` and never merge your own implementation merely because CI is green.
- Any new commit after governor approval invalidates that approval for the previous head.
- Locally run build/compile and focused affected tests sufficient to prove the change, including security/adversarial/mutation evidence when it materially establishes a critical invariant.
- Do not routinely duplicate the complete repository regression matrix locally when hosted PR CI runs it on the exact pushed head. Hosted PR CI is the authoritative full-regression gate before merge.
- Run broader/full suites locally when blast radius is uncertain, build/test/CI infrastructure changes, a hosted failure needs reproduction, hosted CI is unavailable, or repository-specific governance explicitly requires it.
- Independent review may begin while hosted CI is running; merge remains blocked until all required exact-head hosted gates are green.

Unexpected evidence may reopen Product Decisions/Design; implementation convenience may not authorize a breaking public-contract change.

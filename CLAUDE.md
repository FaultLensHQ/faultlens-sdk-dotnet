# CLAUDE.md — faultlens-sdk-dotnet

Read `AGENTS.md` first. This file contains Claude-specific execution notes only.

## Model routing

Follow the distributed `faultlens-engineering` compute/context contract at governance **1.2.7**, released from `FaultLensHQ/faultlens-engineering` commit `c43eb36d81d157858c0ea03c0c355007f45320e5`.

- Use the strongest reasoning tier for Product Decisions, Design, unresolved public API/privacy/compatibility/data-correctness reasoning, difficult debugging and strict independent review.
- Once the approved contract is settled, default routine implementation, remediation and test authoring to a **medium-cost capable execution model**.
- Prefer the **lowest-cost capable mechanical tier** for deterministic/repetitive test/build/pack re-runs, formatting/lint work, straightforward explicit tests, repetitive edits, documentation, CI/log/status inspection and already-specified remediation.
- Do not retain top reasoning merely because the task originated there. A lower tier that encounters ambiguity, contradiction or material risk must stop/escalate rather than guess.
- At coherent work boundaries, checkpoint durable state in GitHub and prefer a fresh execution context when prior conversation state is mostly completed work.
- Compute/context economy must never weaken public API compatibility, privacy, host-application safety, correctness, validation evidence or independent review.

Do not hard-code durable model version names.

## Context and implementation

- Read the tracked issue/decision, current public API/configuration, project/package configuration and smallest affected runtime path first.
- Read target frameworks/language/package versions from current project files rather than stale prose.
- Prefer targeted edits and narrow diffs.
- Preserve host-application failure isolation, async/cancellation behavior and privacy-safe defaults.
- Public API/compatibility changes require explicit approved Product Decision/Design.
- Avoid dependencies/abstractions not required by the approved design.

Persist material decisions/evidence in GitHub rather than workstation-specific paths.

Run repository-defined build/test/pack validation required by the change and report exact results. Do not publish or change package versions unless explicitly authorized.

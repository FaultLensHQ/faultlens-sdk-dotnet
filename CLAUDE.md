# CLAUDE.md — faultlens-sdk-dotnet

Read `AGENTS.md` first. This file contains Claude-specific execution notes only.

## Model routing

Use the strongest reasoning model available for Product Decisions, Design and strict review of public API/privacy/compatibility changes. Use the strongest practical coding/execution model for routine implementation, remediation and testing.

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

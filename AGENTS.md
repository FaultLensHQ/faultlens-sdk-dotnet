# AGENTS.md — faultlens-sdk-dotnet

Repository-local overlay for the official FaultLens .NET SDK.

This file owns SDK/public-contract/safety/build constraints. It must not duplicate current package/framework versions or workstation-specific workflow paths when authoritative project files exist.

## Decision/design routing

For non-trivial SDK changes:

1. establish current public API/runtime behavior and customer integration impact;
2. use Product Decisions when product semantics, capture/privacy policy, compatibility intent, priority or non-goals are unresolved;
3. use Design when the approved decision still needs an implementation-ready public contract/migration/runtime design;
4. implement narrowly;
5. strictly review compatibility, privacy, failure isolation and executable evidence.

**Discovery does not imply priority.** Adjacent cleanup does not automatically become active product work.

## SDK public contract

- SDK behavior must help customers capture useful diagnostic context safely and with minimal integration friction.
- Treat all public exported types/members/configuration as customer-facing contracts.
- Breaking public API changes require explicit Product Decision + migration/versioning design.
- SDK/network/ingestion failures must not escape as unhandled host-application failures.
- Secrets/tokens/passwords/cookies/auth headers/PII are never captured by default.
- Preserve async/cancellation semantics and broad compatibility declared by authoritative project configuration.
- Avoid unnecessary dependencies and runtime footprint.

## Package/build truth

Read package identity, version, target frameworks, language version, warning policy and package-generation behavior from current `.csproj`/solution/configuration. Do not duplicate volatile values here.

Never publish packages or change versions unless explicitly authorized for the active release task.

## Repository discipline

- GitHub issues/PRs are the durable work record.
- Keep changes focused and preserve unrelated work.
- Persist decisions/evidence in GitHub rather than a personal filesystem scratch path.
- Follow the repository's actual branch/release configuration rather than assuming another repo's convention.

## Validation

Run the build/test/pack validation required by the affected public contract using commands from the current solution/project configuration. Packaging validation does not authorize publishing.

Report exact results and anything not validated. Never claim an unexecuted check passed.

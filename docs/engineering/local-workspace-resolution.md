# Local workspace and repository resolution

This downstream repository follows the canonical FaultLens Engineering local-workspace contract from `FaultLensHQ/faultlens-engineering/docs/engineering/local-workspace-resolution.md`, governance **1.2.9**, released source commit `8d60bb16532fa143e45d96380d23a6ca9dd4b0ab`.

Before cloning or reconstructing a FaultLens repository, resolve `FaultLensHQ/<repo>` against the configured local workspace/repository map, verify the candidate Git working tree and remote identity, and reuse the valid local checkout when suitable.

On the current primary Windows workstation, the configured FaultLens workspace root is `C:\PersonalProjects`. Repository-name mapping is the default convention (for example, `FaultLensHQ/faultlens-sdk-dotnet` maps to `C:\PersonalProjects\faultlens-sdk-dotnet`). Treat this as execution-environment configuration, not product or architecture truth.

If a mapped checkout is missing or its Git remote does not correspond to the requested repository, stop and resolve the discrepancy rather than silently cloning into an arbitrary location or operating on the wrong checkout.

GitHub remains authoritative for durable issues, pull requests, review state, hosted CI, remote refs and other remote evidence. Local-first execution does not authorize stale-ref assumptions, destructive cleanup, unrelated branch mutation, or weakening of repository-state safety.

The canonical source document remains authoritative; this local copy exists so repository-local Claude/Codex entrypoints can resolve the contract without rediscovering the governance repository first.

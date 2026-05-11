# AGENTS.md — faultlens-sdk-dotnet

Canonical shared instruction file for all coding agents (ClaudeCode, Codex, and equivalents).
This repo owns the **FaultLens .NET SDK** — the official `FaultLens.SDK` NuGet package that customers install to capture errors and diagnostics and send them to FaultLens.
Read this before starting any task in this repo.

---

## Product principle

- SDK changes should help customers capture useful diagnostic context safely and with minimal friction.
- SDK must not surprise users with unsafe defaults, noisy behavior, or breaking changes.
- SDK failures must never crash customer applications — ingestion errors must be swallowed or surfaced through callbacks, not thrown.
- Every change should support faster production triage/debugging or safer SDK operation.

---

## Work mode

- Aggressive build mode. Implementation-first.
- Minimal, production-safe changes.
- Avoid broad refactors unless explicitly required by the issue.
- Preserve public API compatibility unless the issue explicitly requires a breaking change.

---

## GitHub tracking workflow

- Open a GitHub issue before starting feature work. Do not create duplicate issues.
- Use `C:\PersonalProjects\faultlens-ui\issue-body.md` as the scratch file for issue bodies and comments.
- After validation, update the issue using `gh issue comment` with `--body-file`.
- Do not close issues unless implementation is complete and validated.
- Keep GitHub CLI commands simple.

---

## Repo, branch, and release rules

- Repo name: `faultlens-sdk-dotnet`.
- Branch convention: `master` / `dev` / `test`. Do not use `main`.
- Do not publish NuGet packages unless explicitly requested.
- Do not change the package version (`<Version>`) unless explicitly requested.
- Do not set `GeneratePackageOnBuild=true` — it is intentionally `false`.
- Do not deploy or release unless explicitly requested.

---

## SDK implementation rules

- **Package identity**: `FaultLens.SDK` (NuGet ID), namespace `FaultLens.Sdk`. Preserve both exactly.
- **Target framework**: `netstandard2.1` for broad compatibility. Do not narrow the target or add net-specific targets without explicit approval.
- **Language version**: C# 8.0. Do not use language features that require a higher version.
- **`TreatWarningsAsErrors=true`** — all code must build with zero warnings. Do not suppress warnings to work around this.
- **Public API**: treat `IFaultLensClient`, `FaultLensClient`, `FaultLensOptions`, `IFaultLensRequestScope`, and all public types/members as a stable contract. Do not remove, rename, or change signatures without explicit approval.
- **Dependencies**: avoid adding heavy dependencies. Current runtime dependencies are `System.Diagnostics.DiagnosticSource` and `System.Text.Json` — keep additions minimal and well-justified.
- **No sensitive data by default**: do not capture secrets, tokens, passwords, cookies, or PII by default. Configuration must be explicit and opt-in.
- **Async and cancellation**: preserve `async`/`await` and `CancellationToken` patterns where present.
- **Failure isolation**: network/ingestion failures must not propagate as unhandled exceptions into host application code.
- **Internal surface**: `FaultLens.Sdk.Tests` has `InternalsVisibleTo` access — keep internal types testable without over-exposing them.

---

## Validation expectations

- Run `dotnet build FaultLens.Sdk.sln` to verify the full solution builds cleanly.
- Run `dotnet test FaultLens.Sdk.sln` if tests exist or were changed.
- Run `dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj` only if package metadata or packaging behavior changed — do not publish the output.
- Include exact commands and results in the final response.

---

## Final response format

Max 8 bullets covering:

- Files changed
- What changed and why
- Validation commands and results
- GitHub issue update status
- Follow-up notes (only when useful)

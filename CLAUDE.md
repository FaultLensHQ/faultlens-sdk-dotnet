# CLAUDE.md — faultlens-sdk-dotnet (ClaudeCode)

> **Read [AGENTS.md](AGENTS.md) first.** It is the canonical shared rule file covering product principle, work mode, GitHub tracking, branch/release rules, SDK implementation safety rules, and validation expectations. This file contains only ClaudeCode-specific notes.

---

FaultLens .NET SDK — `FaultLens.SDK` NuGet package for capturing errors and diagnostics from customer .NET applications.

## Solution layout

```
src/
  FaultLens.Sdk/          SDK package (netstandard2.1, C# 8.0)
    IFaultLensClient.cs   public client interface
    FaultLensClient.cs    default implementation
    FaultLensOptions.cs   configuration
    IFaultLensRequestScope.cs
    Breadcrumb*.cs        breadcrumb capture types
    Builders/             request/event builders
    Envelopes/            ingestion envelope types
    Transport/            HTTP delivery layer
    Internal/             internal helpers (InternalsVisibleTo tests)
    SdkInfo.cs

tests/
  FaultLens.Sdk.Tests/    xUnit tests (net10.0, FluentAssertions)

samples/
  FaultLens.Sdk.ConsoleSample/   integration sample
```

## Stack

| Layer | Choice |
|---|---|
| Package | `FaultLens.SDK` v1.0.1 |
| Target | `netstandard2.1` |
| Language | C# 8.0 |
| Testing | xUnit + FluentAssertions (net10.0) |
| Serialization | `System.Text.Json` |
| Diagnostics | `System.Diagnostics.DiagnosticSource` |

## Commands

```bash
dotnet build FaultLens.Sdk.sln               # build entire solution
dotnet test FaultLens.Sdk.sln                # run all tests
dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj  # pack only (do not publish)
```

## Non-obvious conventions

- **`TreatWarningsAsErrors=true`** — the SDK project treats all warnings as errors. Every change must be warning-free before committing.
- **`GeneratePackageOnBuild=false`** — intentional. Do not change this.
- **netstandard2.1 + C# 8.0** — do not use C# 9+ features (records, init, pattern improvements) in `src/FaultLens.Sdk/`. Tests target net10.0 and can use modern C#.
- **Public API is a stable contract** — `IFaultLensClient`, `FaultLensClient`, `FaultLensOptions`, and all public members are customer-facing. Treat them as such.
- **`ImplicitUsings=disable`** in the SDK project — all usings must be explicit.

## ClaudeCode-specific notes

- Stay implementation-first. Avoid broad refactors unless the issue explicitly requires them.
- Read only the files needed for the task before editing. Prefer `Edit` (targeted diff) over full rewrites.
- Preserve public API compatibility — check AGENTS.md before removing or renaming any public type or member.
- Keep diffs narrow — no formatting churn, no unrelated renames, no comments unless the why is non-obvious.
- After validation, update the GitHub issue using `C:\PersonalProjects\faultlens-ui\issue-body.md` and `gh issue comment`.
- Do not publish NuGet packages or change the version unless explicitly requested.

# NuGet Publish Readiness

This checklist prepares the `1.1.0` feature release for the official FaultLens .NET SDK. It adds explicit business-context severity metadata (operation criticality, workflow, and job reserved tags plus helpers) and carries forward the `1.0.2` package-health improvements (Source Link, symbol package, deterministic build).

Locked package identity:

- Package ID: `FaultLens.SDK`
- Version: `1.1.0`
- NuGet organization: `FaultLens`
- NuGet prefix request: `FaultLens.*`
- Repository name alignment: `faultlens-sdk-dotnet`
- Public website: `faultlens.in`
- GitHub repository: `https://github.com/FaultLensHQ/faultlens-sdk-dotnet`

Product availability truth:

- Production marketing is live at `faultlens.in`.
- The real multi-tenant SaaS product is not live in production yet.
- Staging is live and is the active validation environment.
- SDK publishing should not imply production SaaS availability until product production validation is complete.

## Package-health build commands

Run package-health validation from the repository root:

```powershell
dotnet clean .\FaultLens.Sdk.sln
dotnet restore .\FaultLens.Sdk.sln
dotnet build .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true
dotnet test .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true
dotnet pack .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true
```

Use `ContinuousIntegrationBuild=true` for local package-health builds so deterministic build and Source Link metadata are emitted the same way as CI release builds.

Expected outputs:

```text
src\FaultLens.Sdk\bin\Release\FaultLens.SDK.1.1.0.nupkg
src\FaultLens.Sdk\bin\Release\FaultLens.SDK.1.1.0.snupkg
```

## Package artifact rules

- Upload `.nupkg` as the main package.
- Upload `.snupkg` as the symbol package if using a workflow that uploads symbols separately.
- Do not upload extracted package contents.
- Do not commit generated `.nupkg` or `.snupkg` files.
- Windows may show `.nupkg` as "Compressed Archive Folder"; that is normal.

## Validate package metadata

Before upload, inspect the package and confirm:

- `PackageId` is `FaultLens.SDK`
- `Version` is `1.1.0`
- `PackageProjectUrl` is `https://faultlens.in/`
- `RepositoryUrl` is `https://github.com/FaultLensHQ/faultlens-sdk-dotnet`
- `RepositoryType` is `git`
- `PackageLicenseExpression` is `MIT`
- README is included in the package
- symbols are enabled with `snupkg`
- portable PDB exists in the symbol package
- no test assemblies or sample output are included in the package
- no local paths, secrets, or machine-specific NuGet source configuration are included

## Validate with a temporary local source

Use a temporary local NuGet source outside committed files. Do not add `NuGet.config` to this repository or to sample repositories.

Example:

```powershell
dotnet nuget add source .\src\FaultLens.Sdk\bin\Release --name faultlens-local-110
dotnet nuget locals all --clear
cd ..\faultlens-dotnet-samples
dotnet restore
dotnet build
dotnet nuget remove source faultlens-local-110
```

The sample project should keep a normal package reference that matches the intended public release candidate:

```xml
<PackageReference Include="FaultLens.SDK" Version="1.1.0" />
```

If `1.1.0` is already published on NuGet.org before this validation pass, do not overwrite it. Choose the next intended release version and validate that exact version to avoid source/cache ambiguity.

Optional minimal smoke check:

```csharp
using System;
using FaultLens.Sdk;

using var client = new FaultLensClient(
    new FaultLensOptions(
        apiKey: "local-package-smoke-test",
        environment: "local",
        release: "1.1.0",
        serviceName: "package-smoke",
        serviceVersion: "1.1.0"));

client.CaptureMessage("FaultLens local package smoke test");
client.Flush(TimeSpan.FromSeconds(1));
```

The package namespace remains `FaultLens.Sdk`; the public NuGet package ID is `FaultLens.SDK`.

## NuGet.org upload

Official NuGet.org publishing must run from GitHub Actions, not a developer machine.

Preferred authentication is NuGet Trusted Publishing from GitHub Actions. Configure a trusted publisher in the NuGet.org FaultLens organization before pushing the release tag. The workflow uses `NuGet/login@v1` to exchange the GitHub OIDC token for a temporary NuGet API key, then passes that temporary key to `dotnet nuget push`.

The `NuGet/login@v1` `user` value must be the NuGet username that created the trusted publishing policy. This may be different from the package owner or NuGet organization. The current policy creator username is `Suresh_Amudalapalli`.

Trusted Publishing policy values:

- Package ID: `FaultLens.SDK`
- Repository owner: `FaultLensHQ`
- Repository name: `faultlens-sdk-dotnet`
- Workflow file: `.github/workflows/nuget-org-publish.yml`
- Policy creator / `NuGet/login@v1` user: `Suresh_Amudalapalli`
- Environment: none, unless a GitHub environment is intentionally added to the workflow later
- Tag pattern: `sdk-v*.*.*`

Fallback option:

- Use a scoped NuGet.org API key only if Trusted Publishing is unavailable for the FaultLens organization/package. Store it as a GitHub Actions secret and update the workflow in the same reviewed change before release.

Manual release steps after approval:

1. Confirm this checklist is complete and the package version has not already been published.
2. Create and push the release tag from the validated commit:

```powershell
git tag sdk-v1.1.0
git push origin sdk-v1.1.0
```

3. Confirm the `Publish FaultLens SDK to NuGet.org` workflow completes successfully.
4. Confirm both `FaultLens.SDK.1.1.0.nupkg` and `FaultLens.SDK.1.1.0.snupkg` are available from the workflow artifacts and NuGet.org package page.

The workflow is triggered only by tags matching `sdk-v*.*.*`. It validates that the tag version matches the project `PackageVersion`, restores, builds, tests, packs, verifies `.nupkg` and `.snupkg`, uploads artifacts, logs in to NuGet.org through GitHub Actions OIDC / NuGet Trusted Publishing, then publishes both packages using the temporary API key returned by `NuGet/login@v1`.

Emergency manual upload command, only if Trusted Publishing or GitHub Actions is unavailable and explicit approval is given:

```powershell
dotnet nuget push .\src\FaultLens.Sdk\bin\Release\FaultLens.SDK.1.1.0.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\FaultLens.Sdk\bin\Release\FaultLens.SDK.1.1.0.snupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Do not upload to NuGet.org until build, test, pack, package inspection, and consumer validation are complete.

## Pre-publish checklist

- `dotnet clean .\FaultLens.Sdk.sln` passes
- `dotnet restore .\FaultLens.Sdk.sln` passes
- `dotnet build .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true` passes
- `dotnet test .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true` passes
- `dotnet pack .\FaultLens.Sdk.sln -c Release /p:ContinuousIntegrationBuild=true` produces `.nupkg` and `.snupkg`
- package metadata matches this document
- Source Link and portable PDB metadata are present as far as local tooling can verify
- GitHub Actions workflow artifact contains both `.nupkg` and `.snupkg`
- local consumer/sample validation succeeds with the normal package reference

<br />

<p align="center">
  <a href="https://faultlens.in" target="_blank" rel="noopener noreferrer">
    <img src="https://faultlens.in/assets/faultlens_logo_ui.png" alt="FaultLens" height="24" />
  </a>
</p>

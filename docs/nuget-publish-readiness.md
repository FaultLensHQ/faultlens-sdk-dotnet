# NuGet Publish Readiness

This checklist prepares the first public prerelease package for the official FaultLens .NET SDK.

Locked package identity:

- Package ID: `FaultLens.SDK`
- Version: `0.1.0-beta.2`
- NuGet organization: `FaultLens`
- NuGet prefix request: `FaultLens.*`
- Repository name alignment: `faultlens-sdk-dotnet`
- Public website: `faultlens.in`
- npm organization for sibling JS packages: `faultlenshq`
- npm scope for sibling JS packages: `@faultlenshq`

Product availability truth:

- Production marketing is live at `faultlens.in`.
- The real multi-tenant SaaS product is not live in production yet.
- Staging is live and is the active validation environment.
- SDK publishing should not imply production SaaS availability until product production validation is complete.

## Pack locally

From the repository root:

```bash
dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj -c Release -o .\.nupkg
```

Expected outputs:

```text
.\.nupkg\FaultLens.SDK.0.1.0-beta.2.nupkg
.\.nupkg\FaultLens.SDK.0.1.0-beta.2.snupkg
```

## Validate with the local machine feed

The developer machine already has a machine-level local NuGet source configured. Do not add `NuGet.config` to the sample repository, do not commit local feed paths, and do not make the sample project depend on local-only restore behavior.

Pack the SDK, copy the release-candidate package into the configured local feed, clear NuGet cache if needed, then restore/build the sample project normally:

```bash
dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj -c Release -o .\.nupkg
copy .\.nupkg\FaultLens.SDK.0.1.0-beta.2.nupkg <machine-local-nuget-feed>
dotnet nuget locals all --clear
cd ..\faultlens-dotnet-samples
dotnet restore
dotnet build
```

The sample project should keep a normal package reference that matches the intended public release candidate:

```xml
<PackageReference Include="FaultLens.SDK" Version="0.1.0-beta.2" />
```

If `0.1.0-beta.2` is already published on NuGet.org before this validation pass, bump the SDK package version first and validate with the new intended release version, such as `0.1.0-beta.3`, to avoid source/cache ambiguity.

Optional minimal `Program.cs` smoke check:

```csharp
using System;
using FaultLens.Sdk;

using var client = new FaultLensClient(
    new FaultLensOptions(
        apiKey: "local-package-smoke-test",
        environment: "local",
        release: "0.1.0-beta.2",
        serviceName: "package-smoke",
        serviceVersion: "0.1.0-beta.2"));

client.CaptureMessage("FaultLens local package smoke test");
client.Flush(TimeSpan.FromSeconds(1));
```

The package namespace remains `FaultLens.Sdk`; the public NuGet package ID is `FaultLens.SDK`.

## Local feed note

Publishing or installing from a local NuGet source does not reserve the public NuGet.org package ID. The public package ID is reserved only when the package is pushed to NuGet.org by an authorized account or organization.

## NuGet.org push

After final metadata review and local package validation:

```bash
dotnet nuget push .\.nupkg\FaultLens.SDK.0.1.0-beta.1.nupkg --api-key <NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Do not push the `.snupkg` separately unless the NuGet symbol push workflow requires it for the configured account; modern `dotnet nuget push` handles symbols for standard NuGet.org package pushes.

## Pre-publish checklist

- `PackageId` is `FaultLens.SDK`
- `Version` is `0.1.0-beta.2`
- package is marked packable
- `GeneratePackageOnBuild` is `false`
- symbols are enabled with `snupkg`
- README is included in the package
- no test assemblies or sample output are included in the package
- `RepositoryUrl` and `PackageProjectUrl` point to final public URLs
- `dotnet test FaultLens.Sdk.sln -nologo` passes
- local machine-feed validation succeeds with the sample repository's normal package reference

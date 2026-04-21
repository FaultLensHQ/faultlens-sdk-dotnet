# NuGet Publish Readiness

This checklist prepares the first public prerelease package for the official FaultLens .NET SDK.

Locked package identity:

- Package ID: `FaultLens.SDK`
- Version: `0.1.0-beta.1`
- NuGet organization: `FaultLens`
- Repository name alignment: `faultlens-sdk-dotnet`

## Pack locally

From the repository root:

```bash
dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj -c Release -o .\.nupkg
```

Expected outputs:

```text
.\.nupkg\FaultLens.SDK.0.1.0-beta.1.nupkg
.\.nupkg\FaultLens.SDK.0.1.0-beta.1.snupkg
```

## Test from a local NuGet source

Create a scratch consumer outside this repository:

```bash
dotnet new console -n FaultLensSdkPackageSmoke
cd FaultLensSdkPackageSmoke
dotnet nuget add source <repo-root>\.nupkg --name faultlens-local
dotnet add package FaultLens.SDK --version 0.1.0-beta.1 --source faultlens-local
dotnet build
```

Optional minimal `Program.cs` smoke check:

```csharp
using System;
using FaultLens.Sdk;

using var client = new FaultLensClient(
    new FaultLensOptions(
        apiKey: "local-package-smoke-test",
        environment: "local",
        release: "0.1.0-beta.1"));

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
- `Version` is `0.1.0-beta.1`
- package is marked packable
- `GeneratePackageOnBuild` is `false`
- symbols are enabled with `snupkg`
- README is included in the package
- no test assemblies or sample output are included in the package
- TODO metadata values for `RepositoryUrl` and `PackageProjectUrl` are replaced with final public URLs before NuGet.org push
- `dotnet test FaultLens.Sdk.sln -nologo` passes
- local scratch-app install succeeds from `.\.nupkg`

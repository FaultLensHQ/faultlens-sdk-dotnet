# FaultLens .NET SDK

`FaultLens.Sdk` is the .NET client for sending production errors and diagnostic breadcrumbs to FaultLens.

The SDK is designed to be:

- non-blocking
- safe by default
- dependency-light
- usable from plain .NET applications without ASP.NET Core-specific wiring

## Current Day 2 baseline

This repo is in the production-readiness pass for public installability.

What exists today:

- packageable SDK project: `src/FaultLens.Sdk/FaultLens.Sdk.csproj`
- core client for exception and message capture
- breadcrumb and request-scope support
- unit and serialization tests under `tests/FaultLens.Sdk.Tests`

What is not in scope here:

- ASP.NET Core DI/middleware package
- guaranteed delivery or persistent queues
- deep framework-specific integrations

## Requirements

- a .NET application that can consume `netstandard2.1`
- a FaultLens project API key
- network access from your app to the FaultLens ingest endpoint

## Install

```bash
dotnet add package FaultLens.Sdk
```

## Quick start

```csharp
using System;
using FaultLens.Sdk;

var client = new FaultLensClient(
    new FaultLensOptions(
        apiKey: "YOUR_PROJECT_API_KEY",
        environment: "production",
        release: "1.0.0"));

try
{
    throw new InvalidOperationException("Something broke");
}
catch (Exception ex)
{
    client.CaptureException(ex);
}

client.Flush(TimeSpan.FromSeconds(2));
client.Dispose();
```

## Configuration

`FaultLensOptions` is the main setup entry point.

```csharp
var options = new FaultLensOptions(
    apiKey: "YOUR_PROJECT_API_KEY",
    environment: "production",
    release: "1.0.0",
    endpoint: new Uri("https://api.faultlens.io"),
    breadcrumbCapacity: 40);
```

Fields:

- `apiKey`: required project API key from FaultLens
- `environment`: environment label such as `production` or `staging`
- `release`: optional release/build version
- `endpoint`: optional override for non-default FaultLens API environments
- `breadcrumbCapacity`: max in-memory breadcrumbs retained before capture

Default endpoint:

```text
https://api.faultlens.io
```

## Common usage

Capture an exception:

```csharp
client.CaptureException(ex);
```

Capture an exception with a custom fingerprint:

```csharp
client.CaptureException(
    ex,
    fingerprint: "payment-timeout");
```

Capture a message:

```csharp
client.CaptureMessage("Unexpected state reached");
```

Capture a message with delivery feedback:

```csharp
client.CaptureMessage(
    "Cache miss threshold exceeded",
    callback: result =>
    {
        if (!result.Success)
        {
            Console.WriteLine($"{result.ErrorCode}: {result.ErrorMessage}");
        }
    });
```

## Breadcrumbs and request scope

Add breadcrumbs before capture:

```csharp
client.AddStep("checkout", "Payment flow started");
client.AddDecision("checkout", "Retrying gateway call");
```

Use a request scope when you want request lifecycle breadcrumbs:

```csharp
using (var scope = client.BeginRequest("GET", "/orders/{id}"))
{
    try
    {
        // request work
        scope.Complete(statusCode: 200);
    }
    catch (Exception ex)
    {
        scope.Fail(statusCode: 500);
        client.CaptureException(ex);
    }
}
```

## Delivery behavior

The SDK contract is intentionally simple:

- capture methods never throw user-visible exceptions
- capture methods do not block application flow
- events are delivered asynchronously
- delivery callbacks are optional and advisory
- `Flush(...)` is available when you need a bounded shutdown drain

## Error codes

Possible `DeliveryResult.ErrorCode` values:

- `network_error`
- `rate_limited`
- `unauthorized`
- `serialization_failed`
- `unknown`

## Compatibility

- target framework: `netstandard2.1`
- C# language version: `8.0`
- no ASP.NET Core dependency
- no `IHttpClientFactory`
- no Polly dependency

## Local development

Build:

```bash
dotnet build FaultLens.Sdk.sln -nologo
```

Test:

```bash
dotnet test FaultLens.Sdk.sln -nologo
```

Create a local package:

```bash
dotnet pack src/FaultLens.Sdk/FaultLens.Sdk.csproj -c Release
```

## Known gaps in current public readiness

- no framework-specific integration package yet
- no end-to-end published NuGet verification recorded in this repo yet

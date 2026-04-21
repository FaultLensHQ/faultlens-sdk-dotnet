# FaultLens .NET SDK

`FaultLens.SDK` is the official .NET client package for capturing application errors, diagnostic breadcrumbs, and request context, then sending them to FaultLens for investigation.

This package is designed to be:

- non-blocking in application code paths
- safe by default, with capture methods that do not throw user-visible exceptions
- lightweight and usable from plain .NET applications
- independent of ASP.NET Core-specific dependency injection or middleware

## Prerelease Notice

`0.1.0-beta.1` is a prerelease package. The core capture and serialization contracts are available for early integration testing, but APIs may still change before a stable `1.0` release.

## Requirements

- a .NET application that can consume `netstandard2.1`
- a FaultLens project API key
- network access from your application to the FaultLens ingest endpoint

## Install

```bash
dotnet add package FaultLens.SDK --version 0.1.0-beta.1
```

## Quick Start

```csharp
using System;
using FaultLens.Sdk;

using var client = new FaultLensClient(
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

Options:

- `apiKey`: required project API key from FaultLens
- `environment`: environment label such as `production` or `staging`
- `release`: optional release or build version
- `endpoint`: optional override for non-default FaultLens API environments
- `breadcrumbCapacity`: maximum in-memory breadcrumbs retained before capture

Default endpoint:

```text
https://api.faultlens.io
```

## Capturing Errors

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

Capture with optional delivery feedback:

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

## Breadcrumbs

Add breadcrumbs before capture to preserve the path that led to an error:

```csharp
client.AddStep("checkout", "Payment flow started");
client.AddDecision("checkout", "Retrying gateway call");
```

Use a request scope to capture request lifecycle breadcrumbs:

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

## Delivery Behavior

- capture methods do not block application flow
- events are delivered asynchronously
- delivery callbacks are optional and advisory
- `Flush(...)` provides a bounded shutdown drain when needed

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
- no `IHttpClientFactory` dependency
- no Polly dependency

## Package Namespace

The NuGet package ID is `FaultLens.SDK`; the code namespace is `FaultLens.Sdk`.

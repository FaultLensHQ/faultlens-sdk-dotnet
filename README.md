# FaultLens .NET SDK

`FaultLens.SDK` is the official .NET client package for capturing application errors, diagnostic breadcrumbs, and request context, then sending them to FaultLens for investigation.

Version `1.1.0` is the next official release candidate for the SDK package.

## Install

```powershell
dotnet add package FaultLens.SDK --version 1.1.0
```

## Quick Start

Create the client from configuration or environment values. Do not hardcode production API keys in source control.

```csharp
using System;
using FaultLens.Sdk;

var apiKey = Environment.GetEnvironmentVariable("FAULTLENS_API_KEY");
var endpoint = Environment.GetEnvironmentVariable("FAULTLENS_ENDPOINT");

using var client = new FaultLensClient(
    new FaultLensOptions(
        apiKey: apiKey,
        endpoint: new Uri(endpoint),
        environment: "production",
        release: "v1.8.4",
        serviceName: "checkout-api",
        serviceVersion: "2026.06.19"));

try
{
    throw new InvalidOperationException("Payment provider timeout");
}
catch (Exception ex)
{
    client.CaptureException(ex);
}

client.Flush(TimeSpan.FromSeconds(2));
```

## Basic Capture

Capture an exception:

```csharp
client.CaptureException(ex);
```

Capture a message:

```csharp
client.CaptureMessage("Unexpected checkout state reached");
```

Capture with a stable fingerprint:

```csharp
client.CaptureException(
    ex,
    fingerprint: "payment-provider-timeout");
```

Use `Flush(...)` during shutdown or short-lived command-line runs to give queued events time to send.

## Request Scopes

Use a request scope to attach route, method, request status, duration, request ID, correlation ID, and breadcrumbs to events captured during a logical operation.

```csharp
using (var scope = client.BeginRequest(
    method: "POST",
    route: "/api/orders",
    data: new Dictionary<string, object>
    {
        ["requestId"] = "req_123",
        ["X-Correlation-ID"] = "corr_456"
    }))
{
    scope.SetRequestContext(
        url: "https://api.example.com/api/orders",
        referrer: "https://app.example.com/cart",
        userAgent: "Mozilla/5.0");
    scope.SetCorrelationId("corr_456");

    try
    {
        // request work
        scope.Complete(statusCode: 201);
    }
    catch (Exception ex)
    {
        scope.Fail(statusCode: 500);
        client.CaptureException(ex);
    }
}
```

Add breadcrumbs before capture to preserve the path that led to an event:

```csharp
client.AddStep("checkout", "Payment flow started");
client.AddDecision("checkout", "Retrying provider call");
```

## Identity And Context

Use opaque, non-sensitive identifiers:

- `anonymousId`: unauthenticated visitor or session identifier
- `accountId`: business or customer account affected by the event
- `tenantId`: SaaS tenant, workspace, org, or runtime tenant
- `userId`: known user inside the account

Anonymous visitor/session:

```csharp
using (var scope = client.BeginRequest("GET", "/landing"))
{
    scope.SetAnonymousId("anon_abc123");
    client.CaptureMessage("Anonymous landing-page activity");
}
```

Known account and user:

```csharp
using (var scope = client.BeginRequest("POST", "/api/orders"))
{
    scope.SetAccount(
        accountId: "acct_1318",
        tenantId: "tenant_42");
    scope.SetUser("user_9482");

    client.CaptureMessage("Order submitted");
}
```

Set known identity in one call:

```csharp
scope.Identify(
    userId: "user_9482",
    accountId: "acct_1318",
    tenantId: "tenant_42");
```

Identity behavior is mutually exclusive within an active scope:

- calling `SetAnonymousId(...)` clears known account/user identity for that scope
- calling `SetAccount(...)`, `SetUser(...)`, or `Identify(...)` clears `anonymousId` for that scope
- the SDK does not intentionally emit `anonymousId` together with known account/user identity in one active scope

Compatibility note: `SetCustomer(...)` remains for older integrations, but it is obsolete. Prefer `SetAccount(...)`, `SetUser(...)`, or `Identify(...)`. Public SDK examples use `accountId` so users do not need to choose between `customerId` and `accountId`.

## Tags

Tags are for extra custom metadata, not primary account/user/service identity.

Good tag examples:

- feature flag
- plan tier
- queue name
- payment provider
- safe demo scenario

```csharp
scope.SetTag("planTier", "enterprise");
scope.SetTag("paymentProvider", "stripe");
```

Do not put secrets or sensitive PII in tags. Avoid names, emails, phone numbers, raw tokens, API keys, authorization headers, cookies, payment card data, full request bodies, or connection strings.

## Severity Metadata

FaultLens classifies severity from observed signals and never infers business importance from routes, URLs, or stack traces. To mark an event as belonging to a business-critical capability, workflow, job, or operation, set explicit metadata on the request scope — these are the only trusted business-severity signals:

```csharp
scope.SetCapability("checkout", FaultLensCriticality.Critical, operation: "payment-capture");
scope.SetOperationCriticality(FaultLensCriticality.High); // criticality of the operation/route
scope.SetWorkflow("tenant-onboarding");
scope.SetJob("nightly-billing-sync");
```

These map to the reserved keys on `FaultLensReservedTags`: `faultlens.capability`, `faultlens.criticality`, `faultlens.operation`, `faultlens.operation.criticality`, `faultlens.workflow`, and `faultlens.job`. Criticality values should be one of `FaultLensCriticality` (`critical`, `high`, `normal`, `low`); other values are ignored by the backend.

## Release And Environment

Use stable environment labels such as `production`, `staging`, or `development`.

Use `release` and `serviceVersion` to help FaultLens group events observed after deployment, issues first seen after deployment, and release-adjacent changes. The SDK does not claim that a release caused an error.

## ASP.NET Core Support

This SDK currently supports manual/request-scope capture through `BeginRequest(...)` and `IFaultLensRequestScope`.

It does not install ASP.NET Core middleware, does not register `IHttpClientFactory`, and does not automatically capture framework HTTP headers. Pass request IDs, correlation IDs, route data, and safe request context explicitly through request scopes.

Automatic ASP.NET Core middleware/header capture is a future integration follow-up.

## Delivery Behavior

- capture methods do not block application flow
- SDK delivery failures do not throw into normal application code paths
- delivery callbacks are optional and advisory
- `Flush(...)` provides a bounded drain for shutdown and short-lived processes

Possible `DeliveryResult.ErrorCode` values:

- `network_error`
- `rate_limited`
- `unauthorized`
- `serialization_failed`
- `unknown`

## Troubleshooting

- Wrong endpoint: verify `FAULTLENS_ENDPOINT` points to the correct FaultLens ingest/API endpoint for your workspace.
- Invalid or missing API key: verify `FAULTLENS_API_KEY` is configured and belongs to the project you expect.
- Network/firewall issue: confirm the host application can reach the configured endpoint over HTTPS.
- No events visible: make sure the code path actually calls `CaptureException(...)` or `CaptureMessage(...)`; for short-lived apps, call `Flush(...)` before exit.
- Local dev vs production confusion: check the configured `environment` value and filters in FaultLens.

## Compatibility

- target framework: `netstandard2.1`
- C# language version: `8.0`
- NuGet package ID: `FaultLens.SDK`
- code namespace: `FaultLens.Sdk`

<br />

<p align="center">
  <a href="https://faultlens.in" target="_blank" rel="noopener noreferrer">
    <img src="https://faultlens.in/assets/faultlens_logo_ui.png" alt="FaultLens" height="24" />
  </a>
</p>

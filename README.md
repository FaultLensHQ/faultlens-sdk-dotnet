# FaultLens .NET SDK

`FaultLens.SDK` is the official .NET client package for capturing application errors, diagnostic breadcrumbs, and request context, then sending them to FaultLens for investigation.

`FaultLens.SDK 1.1.1` is the current release of the SDK package.

> **FaultLens supports any platform.** Use an official SDK where available, or integrate directly
> using the HTTP ingestion API. This package is the official SDK for .NET.

## Install

```powershell
dotnet add package FaultLens.SDK
```

To pin an explicit version:

```powershell
dotnet add package FaultLens.SDK --version 1.1.1
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

FaultLens classifies severity from observed signals and never infers business importance from routes, URLs, or stack traces. To mark an event as belonging to a business-critical capability, set explicit metadata on the request scope — these are the only trusted business-severity signals:

```csharp
scope.SetCapability("checkout", FaultLensCriticality.Critical, operation: "payment-capture");

// Operation on its own — may name a route, workflow, job, command, or any operation.
scope.SetOperation("nightly-billing-sync");
```

The FaultLens backend consumes exactly three reserved tags on `FaultLensReservedTags`: `faultlens.capability`, `faultlens.criticality`, and `faultlens.operation`. `operation` is a single general-purpose field that may name a route, workflow, job, command, or background operation. Criticality values should be one of `FaultLensCriticality` (`critical`, `high`, `normal`, `low`); other values are ignored by the backend.

> **Deprecated in 1.1.1:** `SetOperationCriticality(...)`, `SetWorkflow(...)`, `SetJob(...)` and the reserved constants `OperationCriticality`, `Workflow`, `Job` were emitted by 1.1.0 but are not consumed by the backend. They are now no-ops retained only for source compatibility. Use `SetCapability(...)` and `SetOperation(...)`. See [docs/capability-metadata.md](docs/capability-metadata.md).

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
- the transport retries transient ingest failures internally (bounded, with backoff); it never retries a terminal outcome

### Retry semantics for non-2xx ingest responses

The ingest endpoint (`docs/ingestion-api.md` in `faultlens-backend`) returns three non-2xx statuses with
specific meaning. Do not treat all non-2xx responses the same — in particular, **429 is not always
retryable**: a 429 carrying `monthly_event_capacity_exhausted` means the workspace's monthly allowance is
used up, and retrying will not help until the next usage period.

| HTTP | `reasonCode` | `DeliveryResult.Reason` | `Retryable` | SDK behavior |
|---|---|---|---|---|
| `409` | `event_identity_conflict` | `DeliveryFailureKind.IdentityConflict` | `false` | Never retried. The event id was already accepted for a different event. |
| `429` | `monthly_event_capacity_exhausted` | `DeliveryFailureKind.CapacityExhausted` | `false` | Never retried. Terminal for the current usage period; `PeriodEndUtc` is surfaced when the backend supplies it. |
| `429` | anything else (or no parseable body) | `DeliveryFailureKind.Throttled` | `true` | Retried with bounded backoff, same as ordinary rate limiting. |
| `503` | any `monthly_event_*` reason | `DeliveryFailureKind.ServiceUnavailable` | `true` | Retried with bounded backoff; the allowance authority could not answer. |
| other `5xx` / network error | — | `DeliveryFailureKind.NetworkError` | `true` | Retried with bounded backoff. |
| other non-2xx | — | `DeliveryFailureKind.Http` / `Unknown` | `false` | Not retried. |

The 429-vs-429 distinction is made from the response body's `reasonCode`, not from the status code alone —
the backend's own ingestion rate limiter returns a different body shape (`{"code":"rate_limited",...}`) than
the capacity-exhaustion response. If a 429/503/409 body cannot be parsed, the SDK fails closed: it never
infers capacity exhaustion or identity conflict without evidence, and falls back to the existing
status-code-only behavior.

On terminal failures, `DeliveryResult.ErrorCode` and `DeliveryResult.ReasonCode` carry the backend's
machine-readable reason when one was returned (e.g. `event_identity_conflict`,
`monthly_event_capacity_exhausted`); `DeliveryResult.Success`/`Retryable` remain the primary fields for
branching in code, and `Reason` gives a typed classification without string matching.

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

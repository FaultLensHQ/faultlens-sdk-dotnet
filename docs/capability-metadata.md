# Explicit capability & criticality metadata

FaultLens classifies issue severity from observed impact signals (affected accounts, event
rate, regressions). It deliberately **never infers business criticality** from route names,
URLs, exception messages, or stack traces — that would produce unexplainable classifications.

To tell FaultLens that an event belongs to a business-critical capability, send explicit
metadata using the reserved tags below. When an event carries
`faultlens.criticality = critical` and the issue affects at least one account, the built-in
`business-critical-capability` rule classifies the issue as **Critical**, and the severity
explanation names the explicit metadata as the source.

## Reserved tags (consumed by the backend)

The FaultLens ingestion contract consumes exactly **three** reserved tags. These are the only
reserved keys promoted onto the issue:

| Tag | Meaning | Constraints |
|---|---|---|
| `faultlens.capability` | Business capability, e.g. `checkout`, `billing-sync` | max 128 chars |
| `faultlens.criticality` | Capability criticality | `critical` \| `high` \| `normal` \| `low`; anything else is ignored |
| `faultlens.operation` | Business operation the event belongs to — may name a **route, workflow, job, command, or background operation**, e.g. `payment-capture`, `GET /api/orders/{id}`, `tenant-onboarding`, `nightly-billing-sync` | max 128 chars |

Constants are available in `FaultLensReservedTags` and `FaultLensCriticality`.

`operation` is intentionally a single, general-purpose field. There is no separate workflow or
job tag: model a workflow, job, or command as the `operation` value.

## Usage

```csharp
// Convenience helper — capability, criticality, and operation in one call.
scope.SetCapability("checkout", FaultLensCriticality.Critical, operation: "payment-capture");

// Operation on its own (route, workflow, job, command, …).
scope.SetOperation("nightly-billing-sync");

// Equivalent raw tags
scope.SetTag(FaultLensReservedTags.Capability, "checkout");
scope.SetTag(FaultLensReservedTags.Criticality, FaultLensCriticality.Critical);
scope.SetTag(FaultLensReservedTags.Operation, "payment-capture");
```

## Semantics

- Metadata is promoted onto the FaultLens issue at ingestion; the latest explicitly tagged
  event wins. Untagged events never clear existing metadata.
- Invalid criticality values are ignored, not coerced.
- Severity stays global and environment-independent — environment never drives severity.
- Do not put sensitive values in these tags; they are subject to standard tag sanitization.

## Deprecated in 1.1.1

`SetOperationCriticality(...)`, `SetWorkflow(...)`, and `SetJob(...)`, along with the reserved
constants `FaultLensReservedTags.OperationCriticality`, `.Workflow`, and `.Job`, are
**deprecated**. They were emitted by 1.1.0 but are **not consumed by the FaultLens backend** —
they had no end-to-end effect. In 1.1.1 the helpers are client-side no-ops retained only for
source compatibility and will be removed in a future major version.

Migrate:

| 1.1.0 (deprecated, no effect) | 1.1.1 |
|---|---|
| `scope.SetWorkflow("tenant-onboarding")` | `scope.SetOperation("tenant-onboarding")` |
| `scope.SetJob("nightly-billing-sync")` | `scope.SetOperation("nightly-billing-sync")` |
| `scope.SetOperationCriticality("high")` | `scope.SetCapability(capability, "high", operation)` |

<br />

<p align="center">
  <a href="https://faultlens.in" target="_blank" rel="noopener noreferrer">
    <img src="https://faultlens.in/assets/faultlens_logo_ui.png" alt="FaultLens" height="24" />
  </a>
</p>

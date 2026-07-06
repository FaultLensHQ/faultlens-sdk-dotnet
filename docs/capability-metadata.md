# Explicit capability & criticality metadata

FaultLens classifies issue severity from observed impact signals (affected accounts, event
rate, regressions). It deliberately **never infers business criticality** from route names,
URLs, exception messages, or stack traces — that would produce unexplainable classifications.

To tell FaultLens that an event belongs to a business-critical capability, send explicit
metadata using the reserved tags below. When an event carries
`faultlens.criticality = critical` and the issue affects at least one account, the built-in
`business-critical-capability` rule classifies the issue as **Critical**, and the severity
explanation names the explicit metadata as the source.

## Reserved tags

| Tag | Meaning | Constraints |
|---|---|---|
| `faultlens.capability` | Business capability, e.g. `checkout`, `billing-sync` | max 128 chars |
| `faultlens.criticality` | Capability criticality | `critical` \| `high` \| `normal` \| `low`; anything else is ignored |
| `faultlens.operation` | Operation/workflow/job name, e.g. `payment-capture` | max 128 chars |

Constants are available in `FaultLensReservedTags` and `FaultLensCriticality`.

## Usage

```csharp
// Convenience helper
scope.SetCapability("checkout", FaultLensCriticality.Critical, operation: "payment-capture");

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

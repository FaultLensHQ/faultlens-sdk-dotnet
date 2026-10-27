# FaultLens SDK privacy collection contract (`sdk-privacy-v1`)

Status: normative for every FaultLens SDK (JavaScript/browser, React, Angular, .NET and future SDKs).
Tracking: #707 (parent epic #705). Backend counterpart: `ingest-privacy-v1` (#708,
`faultlens-backend/docs/privacy/ingest-privacy-policy.md`). Identity: ADR-0006 / #709, execution #718.

This file is kept byte-identical in `faultlens-sdk-js` and `faultlens-sdk-dotnet`
(`docs/privacy/sdk-collection-contract.md`), together with the shared test vectors
`docs/privacy/privacy-text-vectors.v1.json`. A change to either file must be made in both repositories.

## 1. Purpose and layering

The SDK collects the minimum telemetry needed to detect, group and debug errors and to measure their impact.
SDK redaction is **defence in depth**. The backend `ingest-privacy-v1` boundary remains the authority and is applied
to every event regardless of what the SDK did. Nothing in an SDK payload can relax or bypass the backend boundary,
and SDKs send no flag that claims an event is already redacted.

The SDK applies its mandatory protections so that, in the common case, a secret or a recognised personal identifier
never leaves the customer's process, is never held in an SDK retry buffer and is never written to an SDK log.

## 2. Mandatory protections (cannot be disabled)

Mandatory protections run on the **final outgoing payload**, after `beforeSend` / any customer hook, immediately
before serialization. No SDK option, including the legacy JavaScript `sanitize: false`, disables them.

1. **Sensitive key names.** Any object property, tag key, header or query-parameter name that is sensitive has its
   value replaced wholesale with `[Filtered]` and is not descended into. A name is sensitive when, after trimming,
   lower-casing and replacing `-` with `_`, it equals `pwd` or contains one of:
   `authorization`, `bearer`, `cookie`, `password`, `passwd`, `token`, `secret`, `apikey`, `api_key`, `session`,
   `csrf`, `xsrf`, `credential`, `private_key`, `card`, `cvv`, `otp`, `email`, `jwt`, `aadhaar`, `signature`.
   (Same semantics as the backend `IsSensitiveKey`.)
2. **Text rules.** Every free-text string (section 4) is scanned with the rules below, in this order. A match is
   replaced with `[REDACTED:<rule>]`; `auth_scheme` keeps the scheme, `kv_secret` keeps the key and separator, and
   `url_userinfo` keeps `scheme://`. Inputs shorter than 6 characters are not scanned.

   | Rule | Matches | Validation |
   | --- | --- | --- |
   | `private_key` | `-----BEGIN … PRIVATE KEY-----` block (to END marker or end of string) | – |
   | `jwt` | `eyJ…` three-segment token | – |
   | `auth_scheme` | `Bearer` / `Basic` / `Digest` / `Negotiate` / `Token` + 8+ char credential | `Bearer` always; others only when credential-shaped (digit, `=+/`, or mixed case) |
   | `kv_secret` | `key<sep>value` where the key **ends** in password, passwd, pwd, secret, api key, private key, access key, token, authorization, credential(s), cookie, session id or jwt; plain, JSON and JSON-in-string forms | `null`/`undefined`/`true`/`false`/empty and already-masked values are skipped |
   | `aws_key` | `AKIA`/`ASIA`/`AGPA`/`AIDA`/`AROA`/`ANPA`/`ANVA`/`AIPA` + 16 | – |
   | `github_token` | `ghp_ gho_ ghu_ ghs_ ghr_` (36+), `github_pat_` (22+) | – |
   | `slack_token` | `xox[abprs]-…`, `xapp-…` | – |
   | `provider_secret_key` | `sk_`/`rk_` + `live`/`test` (16+), `rzp_live_`/`rzp_test_` (10+) | – |
   | `google_api_key` | `AIza` + 35 | – |
   | `url_userinfo` | `scheme://user:pass@` | – |
   | `email` | `local@domain.tld` | a letter must appear in a domain label before the TLD |
   | `card` | 13–19 digits with optional single space/dash separators | Luhn **and** major-network issuer prefix |
   | `aadhaar` | 12 digits, first 2–9, optional space/dash separators | Verhoeff checksum |

   This is exactly the backend high-precision rule set. The backend-only rules `phone` and `in_pan` are lower
   precision and are deliberately **not** applied in the SDK (they would rewrite customer numeric identifiers); the
   backend still applies them. Placeholders never re-match any rule, so the rules are idempotent and the backend
   sees SDK output as a fixed point.
3. **Object keys** are scanned with the text rules too; a key that changes is renamed to its redacted form (with a
   `#2`, `#3` … suffix on collision). If either the original or the redacted key is sensitive (rule 1), the value is
   `[Filtered]`.
4. **URLs** (page/request URL, referrer, network-breadcrumb URL, stack-frame file): userinfo is removed, query and
   fragment parameters with sensitive names are `[Filtered]`, then the whole string goes through the text rules.
5. **Bounds** (applied before scanning; never reject an event): string leaf 8 192 characters; stack trace at most
   200 frames with method/file at most 1 024 characters each; user agent 512 characters; object depth 10, 100
   properties per object, 100 items per array, 10 000 nodes per payload; at most 100 breadcrumbs. SDK-specific
   bounds that are already tighter stay in force. Every regex is linear or has bounded quantifiers and runs only on
   bounded input; .NET additionally uses a match timeout (a timed-out leaf becomes `[Filtered]`).
6. **Fail closed.** If the mandatory pass throws, the event is **not sent and not retried**, and the original payload
   is never transmitted, buffered or logged. The delivery diagnostics report reason code `sdk_privacy_policy_failed`
   (non-retryable).
7. **No payload logging.** SDK logs and diagnostics never contain event payloads, breadcrumb data, context values,
   tag values, URLs with query strings, or identity values.
8. **Retry/buffer safety.** Retries reuse only the already-redacted payload. No SDK keeps an unredacted copy of an
   event for retry or offline buffering.

## 3. Field classes

| Class | Fields | SDK treatment |
| --- | --- | --- |
| IDENTITY | `userId`, `accountId`, `tenantId`, `customerId` (.NET, obsolete API), `anonymousId` | Trimmed; **not** rewritten (rewriting would collapse distinct users and break ADR-0006 counting). Sent only when the application supplies them. Never logged. Never copied by the SDK into context, tags or breadcrumbs. |
| KEY | `eventId` (SDK-generated), `fingerprint`, `correlationId` | Scanned with the text rules. If a rule would change a caller-supplied value, the SDK sends the redacted value (the backend would otherwise reject the whole event with `ingest_privacy_key_field_sensitive`). Grouping changes only for fingerprints that contained a secret or identifier. |
| FREE_TEXT | message, exception type/message, stack frames, breadcrumb category/message/source/entityType/entityId/data, tags (keys and values), context (keys and values), request/page/client strings, release, serviceName, serviceVersion | Bounded, then mandatory rules 1–4. |
| CONTROLLED | environment, platform, level, breadcrumb layer/type/level, SDK name/version | SDK-chosen or allow-listed; exempt. |

## 4. Default collection

| Data | Purpose (#706) | Default | Notes |
| --- | --- | --- | --- |
| Exception type, message, stack frames | detect/group/debug | on (required) | mandatory rules apply |
| Captured message | debug | on when the app calls capture | mandatory rules apply |
| Page/request URL, path, route (browser) / request URL, method, route, status, duration (.NET) | locate failure | on | URL rules (2.4) |
| Query string | locate failure | on | sensitive parameters `[Filtered]`, text rules |
| Referrer (browser) | navigation diagnosis | **off** (JS `captureReferrer`, opt-in) | cross-site referrers can carry third-party identifiers; .NET sends it only when the integration supplies it, URL rules apply |
| Document title (browser) | page identification | **off** (JS `captureDocumentTitle`, opt-in) | titles frequently contain names, mailbox addresses or record names |
| User agent | browser/runtime-specific debugging | on | bounded to 512 characters; no other device fingerprinting (no screen, language, timezone, geolocation, IP) is collected by any SDK |
| Console breadcrumbs (browser) | debug | `console.error` / `console.warn` on, `console.log` off | mandatory rules apply |
| Network breadcrumbs (browser) | debug | on | method, sanitised URL, status, duration only; headers limited to correlation allow-list; bodies never captured |
| Custom context, tags, breadcrumb data | customer-defined diagnostics | only what the app supplies | bounded; mandatory rules apply; see section 5 |
| Identity (`userId`, `accountId`, `tenantId`, `anonymousId`) | affected-user/account impact (ADR-0006) | **none** until the app calls an identity API | opaque IDs only; see section 6 |

No SDK requests browser permissions, reads cookies or storage for telemetry, or performs network calls during
initialization.

## 5. Custom metadata (context, tags, breadcrumb data)

Custom metadata is accepted, but with explicit bounded semantics: it is treated as untrusted free text, bounded by
section 2.5, key-filtered and text-scanned. Values that are not JSON-representable are stringified or replaced
(`[Unserializable]`, `[Circular]`, `[Truncated]`). The SDK guarantees only what the rules above recognise; it does
not detect arbitrary personal data (names, postal addresses, free-form identifiers). Applications must not put
personal data into custom metadata unless their own purpose requires it.

## 6. Identity and ADR-0006

* SDK identity APIs accept only opaque identifiers (`userId`, `accountId`, `tenantId`, `anonymousId`). There is no
  SDK API for email address, name, phone or other profile attributes, and none will be added.
* Plaintext email/name is not needed to count affected users/accounts. Applications should pass a stable opaque
  internal ID. If an identity value is e-mail-shaped, the SDK still sends it unchanged (rewriting it would merge
  all such users into one) and emits a debug-only diagnostic warning that does not contain the value.
* The SDK does **not** hash or pseudonymize identity. ADR-0006 pseudonymization is a server-side, project-scoped,
  keyed HMAC whose root key never leaves the backend; an SDK-side hash would be a second, unkeyed algorithm and is
  prohibited. Until #718 activates the ADR-0006 writer, the backend persists the received identity values as
  plaintext columns; that residual is owned by #718 / #711, not by the SDK.

## 7. Compatibility and versioning

* Payload shape is unchanged: no field is added to or removed from the V1 ingest envelope.
* Behaviour changes: (a) JavaScript `sanitize: false` no longer disables any protection (the option is deprecated
  and has no effect); (b) JavaScript `captureReferrer` and `captureDocumentTitle` default to `false`; (c) messages,
  exception text, stack frames, tags and fingerprints are now redacted client-side, so issues whose text contained
  a redacted value may regroup once (the backend already groups on redacted text since #708).
* No public API is removed. Releases containing this contract need a minor version bump decided at release time.

## 8. Guarantees and non-guarantees

Guaranteed for payloads produced by an SDK implementing this contract: the transmitted payload, any retried copy and
every SDK log line contain none of the credentials or identifiers the rules in 2.2 recognise, no value under a
sensitive key name, and no string over the bounds.

Not guaranteed: arbitrary free-text personal data; IP addresses; phone numbers and PANs (backend-only); values that
fail a checksum/look-alike filter; non-ASCII e-mail addresses; card/Aadhaar numbers glued to letters or `_`; data
the application deliberately supplies as an identity value. Backend `ingest-privacy-v1` applies its full rule set
regardless.

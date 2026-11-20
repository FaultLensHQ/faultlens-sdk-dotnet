# FaultLens .NET SDK 1.2.0 privacy release

Version 1.2.0 introduces the mandatory sdk-privacy-v1 protection contract.

- Final payload sanitization cannot be disabled and runs after user callbacks.
- Retry payloads are already sanitized.
- Exceptions, stack traces, URLs, tags, breadcrumbs, nested metadata, and sensitive key/value
  patterns follow the shared filtering and size-bound contract.
- A privacy-policy failure prevents delivery rather than sending an unprotected event.

The release preserves the approved payload shape and shared [Filtered] vocabulary. It does not
add client-side identity hashing. Backend ingest-privacy-v1 remains the authoritative boundary.

Release only through the tag-triggered Publish FaultLens SDK to NuGet.org workflow after this
branch receives independent review and its exact-head validation is green.

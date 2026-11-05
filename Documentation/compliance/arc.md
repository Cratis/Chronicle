---
uid: Chronicle.Compliance.Arc
title: Cratis Chronicle PII release in Arc queries
description: Arc's Chronicle integration decrypts marked personal data before intercepted query responses. Configure subjects and authorize access on the backend.
---
# Compliance in Arc applications

Cratis Arc uses Chronicle's read-model release to decrypt personally identifiable information (PII) before intercepted query responses reach clients. Use Cratis Chronicle's personal-data annotations and subject identifiers to protect storage and select the right key. Generated frontend proxies do not configure that protection or authorize access.

## From storage to a query response

Chronicle encrypts PII using a subject's key. A projection's read-model schema must mark its own personal properties with `[PII]`, either directly or through a PII-annotated concept. An annotation on an event does not automatically annotate every read-model property that receives its value.

Arc's Chronicle read-model registration installs `ReadModelInterceptor<TReadModel>`. That interceptor calls `IReadModels.Release(...)` for the model being served, so an ordinary intercepted query does not need handwritten decryption code. Release uses the model's schema and resolved subject; it is not a frontend proxy feature and is not an authorization check.

For example, the data portion of an authorized customer-profile response might look like this after release:

```json
{
    "customerId": "customer-42",
    "companyName": "Example Company",
    "contactEmail": "contact@example.com"
}
```

Here `contactEmail` represents a PII-marked property, while `companyName` is public business data. The example is a response shape, not a storage document or an annotation configuration. The backend must retain the correct subject information so release uses the same identity that encrypted the value.

## Erasure and operational failures

Erasing a subject's key makes genuinely encrypted values unreadable; it does not remove historical plaintext or independently retained copies. A released encrypted string with an erased key becomes empty. Test how your concepts and serializers handle that value rather than assuming every model shape remains usable.

Do not treat a release failure as proof of erasure. Models without applicable metadata can pass through unchanged; service errors can retain the original instance, and transport or deserialization failures can throw. Authorize queries independently and verify the response paths your application exposes.

To configure the integration in your application, continue with [Arc Chronicle compliance](/arc/backend/csharp/chronicle/compliance/).

## See also

- [Read models and PII](read-models.mdx)
- [Marking data as PII](pii.mdx)
- [Erasing a subject](erasing-a-subject.mdx)
- [The encryption key lifecycle](key-lifecycle.mdx)

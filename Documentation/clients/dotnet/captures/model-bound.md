---
title: Model-bound captures
description: Attribute-based capture declarations in the .NET client — source, key, append conditions, and field mappings declared with attributes.
---

Model-bound captures use attributes to declare source, key, append conditions, and field mappings.

## Capture source and key

Apply one source attribute and one key attribute on the capture type:

- `[ApiCapture(api, Poll = ..., Route = ...)]`
- `[WebhookCapture(path)]`
- `[MessageCapture(topic)]`
- `[CaptureKey(property)]`

```csharp
[ApiCapture("InvoicingApi", Poll = "10m", Route = "/invoices")]
[CaptureKey("id")]
public class InvoiceCapture;
```

`ApiCapture` references a named API configuration. If `Route` is not set, the configured API base URL is used directly.

:::note
Authentication is not declared on the capture attribute. It is configured in code where the source is configured, so that secrets never live in the capture declaration. See [Configuring authentication](/chronicle/captures/capture-declaration-language/#configuring-authentication).
:::

## Append conditions on event types

Use one condition attribute on each event type:

- `[WhenPropertyChanged(property)]`
- `[WhenAnyOf(properties...)]`
- `[WhenAllOf(properties...)]`
- `[WhenTransition(property, from, to)]`
- `[WhenAdded]`
- `[WhenRemoved]`

```csharp
[WhenPropertyChanged("status")]
public record InvoiceStatusChanged(
    [MapFrom("$.status")] string Status,
    [MapFromContext("occurred")] DateTimeOffset ChangedAt);
```

## Field mapping attributes

Use on event properties (or primary-constructor parameters):

- `[MapFrom(sourcePath)]` maps from capture payload values
- `[MapFromContext(contextProperty)]` maps from capture runtime context

## When to use model-bound captures

Use this approach when:

- Capture intent should live close to event contracts
- Mapping is mostly direct and attribute-friendly
- You want concise definitions with minimal fluent code

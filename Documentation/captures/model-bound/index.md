# Model-Bound Captures

Model-bound captures use attributes to declare source, key, append conditions, and field mappings.

## Capture source and key

Apply one source attribute and one key attribute on the capture type:

- `[ApiCapture(api, Poll = ..., Auth = ..., Route = ...)]`
- `[WebhookCapture(path, Auth = ...)]`
- `[MessageCapture(topic)]`
- `[CaptureKey(property)]`

```csharp
[ApiCapture("InvoicingApi", Poll = "10m", Auth = "bearer $env.API_TOKEN", Route = "/invoices")]
[CaptureKey("id")]
public class InvoiceCapture;
```

`ApiCapture` references a named API configuration. If `Route` is not set, the configured API base URL is used directly.

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

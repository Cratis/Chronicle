# Declarative Captures

Declarative captures use fluent C# builder APIs to define CDC behavior in code.

## Entry points

- Implement `ICaptureFor`
- Configure the capture in `Define(ICaptureBuilder builder)`

```csharp
public class InvoiceCapture : ICaptureFor
{
    public void Define(ICaptureBuilder builder) => builder
        .FromApi("https://api.example.com/invoices", _ => _
            .PollEvery("10m")
            .WithBearerToken("$env.API_TOKEN"))
        .Key("id")
        .Append<InvoiceStatusChanged>(_ => _
            .WhenPropertyChanges("status")
            .Set(e => e.Status, "$.status")
            .Set(e => e.ChangedAt, "$context.occurred"));
}
```

## Source configuration

- `FromApi(url, configure?)`
- `FromWebhook(path, configure?)`
- `FromMessageTopic(topic)`

API source options:

- `PollEvery(interval)`
- `WithBearerToken(token)`
- `WithAuth(auth)`

Webhook source option:

- `WithAuth(auth)`

## Defining identity

Use `Key(propertyPath)` to define the identity property used when comparing current vs. previous payload.

## Mapping operations

Use `Map(...)` on root, nested, or children scopes:

- `Rename(source, target)`
- `Template(target, template)`
- `Translate(target, source, entries => ...)`
- `Split(source, separator, targets...)`

## Append rules

Use `Append<TEvent>(...)` with one condition and one or more field assignments:

Condition methods:

- `WhenPropertyChanges(property)`
- `WhenAnyOf(properties...)`
- `WhenAllOf(properties...)`
- `WhenTransition(property, from, to)`
- `WhenAdded()`
- `WhenRemoved()`
- `WhenExpression(expression)`

Assignment method:

- `Set(targetPropertyExpression, sourceExpression)`

## Scoped capture sections

- `Nested(objectPath, configure)` for nested object rules
- `Children(collectionPath, identifiedBy, configure)` for child collection rules

Both support local `Map(...)` and `Append<TEvent>(...)` blocks.

## Optional capture metadata

You can set an explicit capture ID using `[Capture("guid")]` on the `ICaptureFor` type.

---
title: Declarative captures
description: Fluent C# builder API for defining CDC captures in code — ICapturer, ICaptureBuilder, source configuration, mapping, and append rules.
---

Declarative captures use fluent C# builder APIs to define CDC behavior in code.

## Entry points

- Implement `ICapturer`
- Configure the capture in `Define(ICaptureBuilder builder)`

```csharp
public class InvoiceCapture : ICapturer
{
    public void Define(ICaptureBuilder builder) => builder
        .FromApi("InvoicingApi", _ => _
            .OnRoute("/invoices")
            .PollEvery("10m"))
        .Key("id")
        .Append<InvoiceStatusChanged>(_ => _
            .WhenPropertyChanges("status")
            .Set(e => e.Status, "$.status")
            .Set(e => e.ChangedAt, "$context.occurred"));
}
```

## Source configuration

- `FromApi(api, configure?)`
- `FromWebhook(path, configure?)`
- `FromMessageTopic(topic)`

API source options:

- `OnRoute(route)`
- `PollEvery(interval)`

`FromApi(api, ...)` references a configured [External Service](/chronicle/external-services/) by name. The External Service holds the base URL and authentication, so no authentication is configured on the API source. If no route is configured, the External Service base URL is used directly.

Webhook source options (webhook sources are inbound and configure their own authentication):

- `WithBasicAuth(username, password)`
- `WithBearerToken(token)`
- `WithOAuth(authority, clientId, clientSecret)`

Authentication is never part of the capture declaration text, so that secrets and tokens do not live in capture definitions.

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

You can set an explicit capture ID using `[Capture("guid")]` on the `ICapturer` type.

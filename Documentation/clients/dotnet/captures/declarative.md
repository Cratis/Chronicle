---
title: Cratis Chronicle captures with C# builders
description: Describe Cratis Chronicle captures with C# builders, connect an API source to event append rules, and understand authoring and execution limits.
---

The Cratis Chronicle .NET capture builder describes how changes in external data become events. Use it to keep the source, identity key, and event append rules together in a C# class.

## Entry points

Implement `ICapturer` and define the capture in `Define(ICaptureBuilder builder)`. `CaptureBuilder.Build()` produces a `CaptureDefinition`, but implementing `ICapturer` does not register or start a capture. `IEventStore` does not expose capture management; the kernel's management contract accepts [Capture Declaration Language (CDL)](/chronicle/captures/capture-declaration-language/) text.

The example below describes a status-change event for invoices keyed by `id`. The `$context.occurred` assignment illustrates syntax the API-polling engine does not accept; executable API declarations must use supported item paths or literals.

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

Choose a source with one of these methods:

- `FromApi(api, configure?)`
- `FromWebhook(path, configure?)`
- `FromMessageTopic(topic)`

The engine runs API-polling captures. It rejects webhook and message-topic sources during validation, so those builder methods describe sources the engine cannot run.

For an API source, configure the route and polling interval:

- `OnRoute(route)`
- `PollEvery(interval)`

Register the HTTP [External Service](/chronicle/external-services/) before using its name in `FromApi(api, ...)`. In the example, `InvoicingApi` identifies that service, `/invoices` selects the data to poll, and `10m` sets a ten-minute polling interval. The service holds the base URL and authentication. If you omit the route, the capture uses the base URL directly. See the [.NET registration example](../external-services.md) to configure the service.

The webhook source builder exposes these authentication options:

- `WithBasicAuth(username, password)`
- `WithBearerToken(token)`
- `WithOAuth(authority, clientId, clientSecret)`

These options do not protect a webhook endpoint. Webhook sources are not read, and the capture management contract carries declaration text rather than the builder's authorization settings. Those settings are not sent to the kernel, stored, or enforced.

## Defining identity

Use `Key(propertyPath)` to identify the same item across observations. With `Key("id")`, Chronicle compares an invoice's current payload with the previous payload for that invoice, rather than comparing unrelated invoices.

## Mapping operations

The builder can describe `Map(...)` operations on root, nested, or children scopes. The engine rejects map operations during validation; these methods describe authoring syntax only:

- `Rename(source, target)`
- `Template(target, template)`
- `Translate(target, source, entries => ...)`
- `Split(source, separator, targets...)`

## Append rules

Use `Append<TEvent>(...)` to name the event to emit, choose a condition, and assign its fields. In the invoice example, a change to `status` selects `InvoiceStatusChanged`, and `$.status` supplies its `Status` value.

Condition methods:

- `WhenPropertyChanges(property)`
- `WhenAnyOf(properties...)`
- `WhenAllOf(properties...)`
- `WhenTransition(property, from, to)`
- `WhenAdded()`
- `WhenRemoved()`
- `WhenExpression(expression)`

The engine rejects expression-based conditions declared with `WhenExpression(expression)`. The other condition methods describe property changes, value transitions, additions, and removals.

Assign fields with `Set(targetPropertyExpression, sourceExpression)`. API captures support item-property paths such as `$.status` and literal values, not `$context` or template expressions. See the [CDL execution limits](/chronicle/captures/capture-declaration-language/#execution-scope-and-limitations) before choosing expressions.

## Scoped capture sections

- `Nested(objectPath, configure)` for nested object rules
- `Children(collectionPath, identifiedBy, configure)` for child collection rules

Both builders expose local `Map(...)` and `Append<TEvent>(...)` blocks. The engine rejects nested and children scopes during validation; only root-level append rules execute.

## Optional capture metadata

The `[Capture("guid")]` attribute carries an explicit capture ID on an `ICapturer` type. The attribute does not register the capture.

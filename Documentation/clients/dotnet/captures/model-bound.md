---
title: Cratis Chronicle capture attributes in .NET
description: Describe Cratis Chronicle capture sources, keys, conditions, and field mappings with C# attributes, and understand what the attributes do not execute.
---

Cratis Chronicle model-bound capture attributes describe how external data changes should become events, keeping source settings and mapping intent beside your C# types.

The .NET client exposes the attributes but does not convert them into a `CaptureDefinition` or register and start a capture. Use [Capture Declaration Language (CDL)](/chronicle/captures/capture-declaration-language/) when you need a definition the kernel can validate and run. The examples below explain attribute metadata, not an automatic registration workflow.

## Capture source and key

Choose one source attribute and one key attribute for the capture type. The key identifies the same item across observations, so Chronicle can compare its current and previous values.

- `[ApiCapture(api, Poll = ..., Route = ...)]`
- `[WebhookCapture(path)]`
- `[MessageCapture(topic)]`
- `[CaptureKey(property)]`

```csharp
[ApiCapture("InvoicingApi", Poll = "10m", Route = "/invoices")]
[CaptureKey("id")]
public class InvoiceCapture;
```

In this declaration, `InvoicingApi` names the External Service, `/invoices` selects its route, and `10m` describes polling every ten minutes. If `Route` is not set, the configured service's base URL is used directly. [Register the HTTP service](../external-services.md) with that same name and configure authentication there, not on the capture attribute. See [Configuring authentication](/chronicle/captures/capture-declaration-language/#configuring-authentication) for the boundary between service credentials and capture declarations.

The engine runs API-polling sources and events sources defined in CDL. It rejects webhook and message-topic sources during validation, even though the corresponding attributes exist.

## Append conditions on event types

Use one condition attribute to describe when each event should be appended:

- `[WhenPropertyChanged(property)]`
- `[WhenAnyOf(properties...)]`
- `[WhenAllOf(properties...)]`
- `[WhenTransition(property, from, to)]`
- `[WhenAdded]`
- `[WhenRemoved]`

`WhenPropertyChanged("status")` describes a change to an invoice's status. The example also shows `MapFromContext` syntax; API polling captures do not support that context assignment, so it is not an executable API capture as written.

```csharp
[WhenPropertyChanged("status")]
public record InvoiceStatusChanged(
    [MapFrom("$.status")] string Status,
    [MapFromContext("occurred")] DateTimeOffset ChangedAt);
```

## Field mapping attributes

Apply mapping attributes to event properties or primary-constructor parameters:

- `[MapFrom(sourcePath)]` describes a value from the capture payload, such as `$.status` in the example.
- `[MapFromContext(contextProperty)]` describes a value from capture runtime context.

The attributes record these paths; they do not perform the mapping themselves. For executable declarations, check the [CDL execution limits](/chronicle/captures/capture-declaration-language/#execution-scope-and-limitations): API captures support item-property paths and literals, while events captures also support `$context.<property>` and `$eventSourceId`.

## When to use model-bound captures

Attributes keep capture intent close to event contracts when you are describing direct field mappings. If you need to construct a `CaptureDefinition` in C#, use the [fluent builder](declarative.md). To manage a capture through the kernel, use CDL rather than expecting attributes on a class to activate it.

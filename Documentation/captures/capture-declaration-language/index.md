---
title: "Capture Declaration Language syntax"
description: "Use Capture Declaration Language (CDL) to define Cratis Chronicle captures, with source syntax, append rules, and engine execution limits."
---

Capture Declaration Language (CDL) defines how Cratis Chronicle turns external data changes into events. Use this indentation-based text format to choose a source, identify items, and declare which events to append when their data changes.

## Overview

A CDL definition compiles to a `CaptureDefinition`. The language can describe the following elements, but the engine executes only the subset listed below:

- Source declarations (`api`, `webhook`, `message`, `events`)
- Key declaration to identify items across observations
- Optional map operations (`translate`, `split`, field rename, template assignment)
- Event append rules with `when` conditions
- Nested object scopes
- Child collection scopes

## Execution scope and limitations

The engine reads `api` and `events` sources and executes root-level `append` rules. It does not execute `webhook` or `message` sources, `map` operations, `nested` scopes, or `children` scopes. These constructs can compile, but validation reports them as unsupported and prevents the capture from starting. Expression-based `when` conditions are also rejected during validation.

Field assignments can read item properties (`$.path`) or use string, numeric, and boolean literals. For an `events` source, assignments can also read `$context.<property>` and `$eventSourceId`. API sources do not provide those expressions. Assignments using `$previous`, `$env`, or templates are unsupported.

Validate the declaration before starting it. A successful compilation checks the language structure; it does not establish that the engine can run the definition.

## Example

This capture polls `InvoicingApi` every ten minutes and appends `InvoiceStatusChanged` for each invoice whose `status` changed. Configure the External Service and register the event type before starting it:

```cdl
capture InvoiceCapture
  source api
    api InvoicingApi
    route /invoices
    poll 10m
  key id
  append InvoiceStatusChanged
    when status
    status = $.status
```

The next example illustrates additional language syntax, not an executable capture. Its `map`, `nested`, and `children` sections are unsupported by the engine, and `$context.occurred` is not available to an API source:

```cdl
capture InvoiceCapture
  source api
    api InvoicingApi
    route /invoices
    poll 10m
  key id
  map
    status = status translate
      "utkast" => draft
      "betalt" => paid
  append InvoiceStatusChanged
    when status
    status = $.status
    changedAt = $context.occurred
  nested billingAddress
    append InvoiceBillingAddressChanged
      when street or city
      street = $.billingAddress.street
  children lineItems identified by lineNumber
    append InvoiceLineItemAdded
      when added
      lineNumber = $.lineNumber
    append InvoiceLineItemRemoved
      when removed
      lineNumber = $.lineNumber
```

## Language elements

### Header

`capture <Name>` starts a single capture definition.

### Source block

```cdl
source api|webhook|message|events
  ...
```

Source properties:

- API: `api`, `route`, `poll`
- Webhook: `path`
- Message: `topic`
- Events: `from` (repeatable)

For API sources, `api` identifies a configured [External Service](../../external-services/index.md) by name. The External Service holds the base URL and the authentication for the connection. `route` is optional and is appended to that base URL; if omitted, the base URL is used as-is. An absolute route is accepted only when it has the External Service's scheme, host and port.

> [!NOTE]
> Configure API authentication on the referenced External Service, not in CDL text. Webhook sources do not support authentication. See [Configuring authentication](#configuring-authentication) below.

### Events source

Use an `events` source to translate another event store's public events into private events in your local event log. The capture reads those public events from your store's inbox.

```cdl
capture ShipmentTracking
  source events
    from ShipmentDispatched
    from ShipmentDelivered
  key $eventSourceId
  append OrderShipped
    when status from "pending" to "dispatched"
      orderId = $.orderId
      shippedAt = $context.occurred
```

#### Register the incoming and appended event types

Each `from` line names an incoming event type registered as `Public` with an origin. Repeat the line for each type you want to capture. Private types and types with unspecified visibility are rejected, including registrations from clients that predate visibility. Register a type without an origin with the `[EventStore]` of its owner.

All incoming types must share one nonempty origin. Chronicle derives the inbox name, `inbox-<origin>`, from that registration. Split events from different origins into separate captures. An events capture can read only an inbox, not the event log, outbox, system sequence, or another private sequence. The event types you append must also be registered, must not be `Public`, and must not also appear in `from` lines.

#### Match changes and assign fields

`key $eventSourceId` groups incoming events by their event source. Each event's properties are merged into the previous state for that key and compared with it. A `when status from "a" to "b"` rule fires when that merged state moves `status` between those values; the first event for a key is an `added` change. In field assignments, `$.path` reads this merged state.

Use `$context.<property>` to read the incoming event's metadata, including `eventSourceId`, `eventType`, `sequenceNumber`, `occurred`, `correlationId`, and `subject`. The appended private event records a local fact at the time the capture handles it. Its causation metadata identifies the incoming event through `sourceSequence`, `sourceSequenceNumber`, `sourceEventType`, and `sourceEventSourceId`, and preserves the origin's occurrence time as `sourceOccurred`. The incoming correlation id carries over.

#### Follow processing and recovery

An events capture observes its inbox rather than polling, so do not set `poll`. It catches up and resumes through Chronicle's observer mechanism. Each namespace has its own remembered state, and appends go to the event log in the namespace where the incoming event arrived. A missing key or rejected append fails the observer partition, the unit of processing tracked for recovery, rather than skipping the event.

Each appended event carries a tag identifying its incoming event. On redelivery, the capture checks for that tag in the local log and does not append again if matching events already exist.

Starting subscribes the capture in every namespace before recording it as started. If any subscription fails, Chronicle unsubscribes those already established and the start fails. Started captures are subscribed again when the kernel starts and in namespaces added later. While the captures manager is active, it checks every minute for lost subscriptions and attempts to recover them; subscription failures are logged as errors and retried.

Stopping unsubscribes the capture. Deleting also removes its observers, their offsets and definition, and the state remembered in each namespace.

:::note[Legacy declarations]
`sequence inbox-<event store>` is deprecated. Existing declarations still compile without rewriting their stored text, and new declarations containing it are accepted too. The named inbox must match the origin of every `from` event type. Chronicle logs a deprecation warning once per declaration per kernel process. Remove the `sequence` line when updating a declaration; use only `from` lines for new captures.
:::

### Configuring authentication

API sources connect through a configured [External Service](/chronicle/external-services/). Set the base URL and authentication on that service, then reference it by name in the capture. The [Declarative Captures](/chronicle/clients/dotnet/captures/declarative/) example shows that reference in the .NET client's builder API.

Webhook sources are not read by the engine and do not support authentication. The capture contract carries only the declaration text. The .NET source builder's `WithBasicAuth`, `WithBearerToken` and `WithOAuth` methods are obsolete (they still compile with a warning): their settings are not sent to the kernel, stored or enforced, so remove those calls and do not rely on them to protect an endpoint.

### Key directive

Use `key <propertyPath>` to identify each item across observations. An events capture can use `key $eventSourceId` to identify items by the incoming event's event source.

### Map block

The `map` syntax describes transformations before append rules. The engine does not execute this phase:

- field rename: `target = source`
- template assignment: ``target = `template ${expr}```
- translate: `target = source translate` + value entries
- split:

  ```cdl
  split source by ","
    first
    second
  ```

### Append block

```cdl
append <EventType>
  when ...
  <targetField> = <sourceExpression>
```

The engine evaluates property changes, value transitions, and additions or removals. The language accepts these `when` forms; only the final expression-based form is unsupported by the engine:

- `when property`
- `when p1 or p2`
- `when p1 and p2`
- `when property from old to new`
- `when added`
- `when removed`
- `when \`expr\``

### Nested block

A `nested` block scopes rules to an object. It is part of the language, but the engine rejects it during validation.

```cdl
nested <objectPath>
  [map ...]
  append ...
```

### Children block

A `children` block describes rules for a collection, with a key identifying each child. The engine rejects this block during validation.

```cdl
children <collectionPath> identified by <childKey>
  [map ...]
  append ...
```

## Expressions

CDL can represent the following source expressions. Use `$.path` in either executable source type; `$context.<property>` and `$eventSourceId` are available only to events captures. The engine does not support `$previous` or `$env` assignments:

- `$.path` (current payload)
- `$previous.path` (previous payload)
- `$context.occurred` (capture context; for an `events` source, the context of the incoming event)
- `$eventSourceId` (events sources: the event source of the incoming event)
- `$env.VARIABLE` (environment lookup)

## Formal language specification

Use the [Grammar (EBNF)](grammar.md) to look up the exact syntax after choosing constructs within the execution limits above.

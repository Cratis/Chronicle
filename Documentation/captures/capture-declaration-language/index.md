---
title: "Capture Declaration Language"
description: "Syntax of the Capture Declaration Language, and which parts the capturing engine runs today."
---

The Capture Declaration Language (CDL) is an indentation-based DSL for defining captures that transform external data changes into Chronicle events.

## Overview

CDL definitions compile to `CaptureDefinition` and support:

- Source declarations (`api`, `webhook`, `message`)
- Key declaration for identity and diffing
- Optional map operations (`translate`, `split`, field rename, template assignment)
- Event append rules with `when` conditions
- Nested object scopes
- Child collection scopes

:::caution[The capturing engine runs a subset of the language]
Captures parse and compile everything described here, but as of Chronicle 19.6 the engine that runs them supports only part of it:

- Only `api` and `events` sources are read. A capture with a `webhook` or `message` source never produces events.
- Only root-level `append` rules run. `map` operations, `nested` scopes, and `children` scopes are accepted but not applied.
- An assignment can take a property of the item (`$.path`) or a quoted literal. `$context`, `$env`, and template expressions are rejected at run time, and so are expression-based `when` conditions.

A cycle that hits an unsupported construct fails as a whole and is logged; no event is appended for it.
:::

## Example

This capture polls an API every ten minutes and appends `InvoiceStatusChanged` for each invoice whose `status` changed. It uses only what the engine runs today:

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

The full language adds `map` operations, `nested` and `children` scopes, and context expressions. The sections below describe their syntax, which compiles today:

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

- `capture <Name>` defines one capture.

### Source block

```cdl
source api|webhook|message|events
  ...
```

Source properties:

- API: `api`, `route`, `poll`
- Webhook: `path`
- Message: `topic`
- Events: `sequence`, `from` (repeatable)

For API sources, `api` identifies a configured **[External Service](../../external-services/index.md)** by name. The External Service holds the base URL and the authentication for the connection. `route` is optional and is appended to that base URL; if omitted, the base URL is used as-is.

> [!NOTE]
> Authentication is **not** part of the CDL. For API sources it is configured on the referenced External Service; for webhook sources it is configured in code on the source builder. Either way, secrets and tokens never live in capture text. See [Configuring authentication](#configuring-authentication) below.

### Events source

An `events` source captures from the **public events** of another event store that arrive in this event store's inbox, and appends **private events** to the local event log.

```cdl
capture ShipmentTracking
  source events
    sequence inbox-fulfillment
    from ShipmentDispatched
    from ShipmentDelivered
  key $eventSourceId
  append OrderShipped
    when status from "pending" to "dispatched"
      orderId = $.orderId
      shippedAt = $context.occurred
```

- `sequence` names the inbox, `inbox-<event store>`. It is optional: when it is left out, the inbox is derived from the origin the `from` types were registered with, which requires every `from` type to share one origin - otherwise start is rejected and the inbox must be named. Only an inbox can be captured from; the event log, the outbox, the system sequence and any other private sequence are rejected when the capture is started.
- `from` names a registered event type to capture. It must be registered as `Public` and with an origin that matches the inbox's event store; private types, types of unspecified visibility (clients that predate visibility are not accepted) and types that originate elsewhere are rejected. Repeat it for each type. The incoming event content is the captured item (`$.path`), and the event context is available as `$context.<property>` (`eventSourceId`, `eventType`, `sequenceNumber`, `occurred`, `correlationId`, `subject`). `key $eventSourceId` keys the capture on the incoming event's event source.
- There is no `poll`. The capture is an observer of the inbox, so it catches up, resumes and fails a partition the way other observers do.
- The state per key is what the key's incoming events have said so far: each event's properties are laid over the previous state, then compared with it. A `when status from "a" to "b"` rule therefore fires when an event moves `status` between those values. The first event for a key is an `added` change.
- Appended event types must be registered, must not be `Public` and must not also be `from` types. A capture appends private events of this event store.
- The appended events are a local fact recorded when the capture handled the event. When the incoming event occurred at its origin is kept in the causation (`sourceOccurred`), along with `sourceSequence`, `sourceSequenceNumber`, `sourceEventType` and `sourceEventSourceId`. The correlation id of the incoming event carries over.
- Capturing is idempotent. Each appended event is tagged with the incoming event it came from, and an incoming event that already has appended events is not translated again when it is redelivered.
- Every namespace of the event store is captured separately, each with its own remembered state. Appends go to the event log of the namespace the incoming event arrived in.
- A failure - an event without a key, an append that is rejected - fails the partition rather than skipping the event.
- Starting subscribes the capture in every namespace before it is recorded as started. If any namespace fails, the ones already subscribed are unsubscribed again and the start fails.
- A started capture stays subscribed: it is subscribed again when the kernel starts, in namespaces added later, and a periodic reconciliation (every minute while the captures manager is active) recovers a subscription lost to a restart, deactivation or failed setup. A failure to subscribe is logged as an error and retried; it is not silent.
- Stopping unsubscribes the capture. Deleting also removes its observers, their offsets and definition, and the state it remembered in each namespace.

### Configuring authentication

**API sources** connect through a configured [External Service](/chronicle/external-services/). Configure the base URL and authentication once on the External Service; the capture simply references it by name — see the [Declarative Captures](/chronicle/clients/dotnet/captures/declarative/) example for what that reference looks like in the .NET client's builder API.

**Webhook sources** are inbound and configure their authentication in code on the source builder:

- `WithBasicAuth(username, password)` — basic authentication
- `WithBearerToken(token)` — bearer token authentication
- `WithOAuth(authority, clientId, clientSecret)` — OAuth authentication

When no authentication is configured, the source is treated as unauthenticated.

### Key directive

- `key <propertyPath>`

### Map block

`map` supports:

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

Supported `when` forms:

- `when property`
- `when p1 or p2`
- `when p1 and p2`
- `when property from old to new`
- `when added`
- `when removed`
- `when \`expr\``

### Nested block

```cdl
nested <objectPath>
  [map ...]
  append ...
```

### Children block

```cdl
children <collectionPath> identified by <childKey>
  [map ...]
  append ...
```

## Expressions

Typical source expressions:

- `$.path` (current payload)
- `$previous.path` (previous payload)
- `$context.occurred` (capture context; for an `events` source, the context of the incoming event)
- `$eventSourceId` (events sources: the event source of the incoming event)
- `$env.VARIABLE` (environment lookup)

## Formal language specification

See [Grammar (EBNF)](grammar.md) for the full formal syntax.

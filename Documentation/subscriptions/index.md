---
title: Chronicle subscriptions between event stores
description: Chronicle subscriptions forward published events between event stores so services can react to changes while keeping their own event history.
---

A Cratis Chronicle subscription forwards events from one event store's outbox into another's inbox. Use subscriptions when a service needs to react to events published by another service without sharing its event store.

## Core Concepts

The source service publishes events to its outbox, an event sequence intended for other stores to consume. Chronicle forwards matching events into a separate inbox sequence in the target store for each source. Reactors, projections, and reducers can then process those inbox events in the consuming service.

The kernel owns the forwarding subscription, not the client connection. A registered subscription survives client disconnections and is re-established when the kernel starts. Consumer processing is a separate step: an event can be present in the inbox even if a projection has not yet updated its view.

### Choosing Between Implicit and Explicit Subscriptions

Use [implicit subscriptions](implicit-subscriptions) when a C# consumer shares event contracts annotated with `[EventStore("service-name")]`, often through a NuGet package. Chronicle infers the source from the event types handled by its observers and registers the forwarding subscription.

Use [explicit subscriptions](explicit-subscriptions) when you want to choose a subscription ID, select event types, or manage the relationship at runtime. The consuming service registers the subscription through the client API rather than relying on C# attributes.

Both approaches use the same outbox-to-inbox flow. Start with [Outbox and Inbox](outbox-inbox) if you need to decide what to publish or where the consumer should observe it.

## Topics

- [Implicit Event Store Subscriptions](implicit-subscriptions) — route C# observers using source-store attributes on shared event contracts
- [Explicit Event Store Subscriptions](explicit-subscriptions) — register and remove subscriptions with the client API
- [Outbox and Inbox](outbox-inbox) — follow an event from publication to consumer processing
- [Compliance and PII](compliance-and-pii.md) — understand subject identity, key propagation, and erasure during forwarding
- [Subscription recovery](recovery.mdx) — distinguish a forwarding failure from a consumer-processing failure before replaying events

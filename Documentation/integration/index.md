# Integration

Chronicle supports integrating multiple event stores, allowing events to flow between them in a controlled, persistent, and observable way. This is particularly useful in distributed systems where different services own separate event stores but need to react to each other's events.

## Core Concepts

Integration in Chronicle is built around the **outbox/inbox pattern** and two complementary subscription modes:

- **Outbox** — an event store publishes events to a well-known sequence so other stores can subscribe
- **Inbox** — an event store maintains one inbox sequence per source it subscribes to, receiving forwarded events automatically
- **Implicit Subscriptions** — automatic routing based on `[EventStore]` attributes on event types
- **Explicit Subscriptions** — manual subscription setup using the `Subscribe()` API

Chronicle manages the subscription lifecycle on the Kernel side. Subscriptions survive client disconnections and are automatically re-established when the Kernel starts.

### Choosing Between Implicit and Explicit Subscriptions

**Implicit subscriptions** are best when event types are published in a NuGet package. The source service annotates events with `[EventStore("service-name")`, and consuming services simply reference the package and observe the events—Chronicle handles routing automatically.

**Explicit subscriptions** give you fine-grained control. Use them when you need to filter specific event types, when events are not in a shared package, or when subscription configuration must be dynamic.

## Topics

- [Implicit Event Store Subscriptions](implicit-subscriptions.md) — automatic routing via `[EventStore]` attributes in shared NuGet packages
- [Explicit Event Store Subscriptions](explicit-subscriptions.md) — manual subscription setup using the `Subscribe()` API
- [Outbox and Inbox](outbox-inbox.md) — the outbox/inbox pattern and how events flow between stores

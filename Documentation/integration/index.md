# Integration

Chronicle supports integrating multiple event stores, allowing events to flow between them in a controlled, persistent, and observable way. This is particularly useful in distributed systems where different services own separate event stores but need to react to each other's events.

## Core Concepts

Integration in Chronicle is built around two complementary patterns:

- **Outbox** — an event store publishes events to a well-known sequence so other stores can subscribe
- **Inbox** — an event store maintains one inbox sequence per source it subscribes to, receiving forwarded events automatically

Chronicle manages the subscription lifecycle on the Kernel side. Subscriptions survive client disconnections and are automatically re-established when the Kernel starts.

## Topics

- [Event Store Subscriptions](event-store-subscriptions.md) — how to set up and manage subscriptions between event stores
- [Outbox and Inbox](outbox-inbox.md) — the outbox/inbox concept and how events flow between stores
- [Subscribing to External Event Stores](event-store-attribute.md) — annotating event types with `[EventStore]` for NuGet packages and automatic inbox routing

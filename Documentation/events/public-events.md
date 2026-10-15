---
title: Public events
description: Declare event types as public or private, and how the kernel keeps private events out of the outbox and public events out of the event log.
---

An event type is either **public**, part of the contract an event store exposes to other services, or **private**, local to the event store that owns it. Chronicle records that visibility when a client registers its event types and enforces it when events are appended.

## Declaring visibility

The .NET client derives the visibility of each event type when it registers:

| Declaration | Registered as |
| --- | --- |
| `[Public]` on the event type | Public |
| `[EventStore("name")]` on the event type or its assembly, naming this event store or another one | Public, with that event store as its origin |
| Neither | Private |

An event type that names another event store is a public event of *another* service, consumed through the inbox. Its origin is stored with the event type, so tooling can tell where it comes from.

## What the kernel enforces

Every append to an event sequence looks up the registered visibility of the event type:

| Event sequence | Private | Public | Unspecified |
| --- | --- | --- | --- |
| Outbox | Refused with `PrivateEventTypeCannotBeAppendedToOutbox` | Appended | Appended, with a warning |
| Event log | Appended | Refused with `PublicEventTypeCannotBeAppendedToEventLog` | Appended |
| Any other sequence | Appended | Appended | Appended |

A public event type is not tied to the outbox. It can be appended to, and published to, any other event sequence, and a sequence other than the outbox and the event log applies no visibility check. Only the event log refuses it.

Public events are *produced* from private events, for example by a projection or reducer that publishes to the outbox, rather than appended to the event log as facts. The rule applies to a single append and to appending many events; when any event in a batch is refused, none of the batch is written.

The refusal is returned in the errors of the append result, the same way a schema violation is reported. Nothing is thrown.

## Clients that predate visibility

A client that predates public events sends no visibility, which the kernel stores as **Unspecified**. Unspecified event types keep behaving as before so existing systems keep working: appending them to the outbox or the event log is allowed. The kernel logs a warning the first time such an event type is appended to the outbox or event log after a sequence is activated. Upgrade the client to have visibility enforced.

A registration that sends no visibility never overwrites a visibility that has already been stored. An older client reconnecting does not turn a public event type back into an unspecified one.

## Storage

Visibility and origin are stored with the event type in every storage provider:

| Provider | Stored as |
| --- | --- |
| In-memory | Kept with the registered event type for the lifetime of the process |
| MongoDB | `visibility` and `origin` fields on the event type document. Documents written earlier have neither and read back as unspecified |
| SQL | `Visibility` and `Origin` columns on the event types table, added by migration `v19_36_0`. Existing rows get unspecified and no origin |

## Related

- [Sinks and event publication](../sinks/index.mdx)
- [Implicit subscriptions](../subscriptions/implicit-subscriptions.mdx)

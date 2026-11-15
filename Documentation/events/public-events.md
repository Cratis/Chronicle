---
title: Declare Chronicle public events and enforce visibility
description: Declare Chronicle event types as public or private to expose a service contract while keeping private events out of the outbox and public events out of the log.
---

Public events form the contract a Chronicle event store exposes to other services. Private events remain local to their owning event store. Declare visibility to keep internal events out of the outbox and public contract events out of the event log; Chronicle records it during event type registration and checks it when appending.

## Declaring visibility

In the .NET client, attribute-based declarations determine visibility as follows:

| Declaration | Registered as |
| --- | --- |
| `[Public]` on the event type | Public |
| `[EventStore("name")]` on the event type or its assembly, naming this event store or another one | Public, with that event store as its origin |
| Neither | Private |

An event type that names another event store represents that service's public contract, consumed through the inbox. Chronicle stores the named event store as the type's origin so tooling can identify where it comes from.

If you use explicit event type registration, `.Public()` and `.Private()` declare visibility directly. Without an explicit visibility declaration, a named event store makes the type public; a type with neither declaration is private.

## What the kernel enforces

The destination sequence determines which registered event types it accepts:

| Event sequence | Private | Public | Unspecified |
| --- | --- | --- | --- |
| Outbox | Refused with `PrivateEventTypeCannotBeAppendedToOutbox` | Appended | Appended, with a warning |
| Event log | Appended | Refused with `PublicEventTypeCannotBeAppendedToEventLog` | Appended, with a warning |
| Any other sequence | Appended | Appended | Appended |

Record private facts in the event log, then produce public events from them, for example through a projection or reducer that publishes to the outbox. Public event types can also be appended or published to other sequences: sequences other than the outbox and event log apply no visibility check.

Visibility checks apply to single appends and batches. If any event in a batch fails the check, Chronicle writes none of the batch. Inspect the append result's errors for the refusal code shown above; a visibility refusal returns a failed result instead of throwing an exception.

## Clients that predate visibility

Older clients send no visibility, so a newly registered type has `Unspecified` visibility. Chronicle allows these types in both the outbox and the event log for compatibility. The kernel logs a warning on the first such append per event type after a sequence is activated. Upgrade the client and declare visibility to enforce the public/private boundary.

A registration with no visibility preserves any visibility already stored. Reconnecting an older client therefore does not turn a public event type back into an unspecified one.

## Storage

Each storage provider keeps visibility and origin with the event type:

| Provider | Stored as |
| --- | --- |
| In-memory | Kept with the registered event type for the lifetime of the process |
| MongoDB | `visibility` and `origin` fields on the event type document. Documents written earlier have neither and read back as unspecified |
| SQL | `Visibility` and `Origin` columns on the event types table, added by migration `v19_36_0`. Existing rows get unspecified and no origin |

## Related

To produce public events from private facts, continue with [Sinks and event publication](../sinks/index.mdx). For how another service consumes them through its inbox, see [Implicit subscriptions](../subscriptions/implicit-subscriptions.mdx).

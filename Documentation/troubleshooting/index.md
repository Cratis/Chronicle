---
title: Troubleshooting
description: Answers to the questions that come up most often when something isn't behaving the way you expected.
---

Most "it's not working" moments with Chronicle come down to a handful of causes. This page collects them. If your issue isn't here, the [Glossary](/chronicle/concepts/glossary/) and the feature guides go deeper.

## My read model is empty or out of date

A read model is built by a [projection](/chronicle/concepts/projection/) that runs *after* events are appended — it is [eventually consistent](/chronicle/read-models/), not instant. A few things to check, in order:

- **Did the events actually get appended?** Look at the event sequence. No events in, no read model out.
- **Did you give the projection time?** Immediately after appending, the projection may not have processed yet. For tests, wait for the read model rather than asserting instantly.
- **Does the projection map the events you appended?** If the read model only handles `BookAdded` but you appended `BookBorrowed`, that event won't change it.
- **Is the property mapping right?** AutoMap matches by name. A `Title` on the event maps to `Title` on the read model; a mismatch means the value silently doesn't flow.

## My read model's child collection is null

A child collection with no children is stored as an absent field rather than as `[]` — deliberately, so that a parallel replay cannot erase a child a sibling event already added. Chronicle fills that absence in for you when the property is declared non-nullable, and leaves it as `null` when you declared it nullable to keep "no children yet" distinguishable. Two things do not close the gap: [initial values](/chronicle/projections/declarative/initial-values/) are dropped for a children collection, and reading the document with the MongoDB driver instead of through Chronicle bypasses the rule entirely. See [Empty child collections](/chronicle/read-models/empty-child-collections/).

## My projection isn't picking up a change I made

After you change a projection, the existing read model still reflects the *old* logic. Rebuild it by **replaying** — re-running the projection over historical events. Because events are the source of truth, read models are disposable and safe to rebuild at any time.

## My reactor ran twice (or sent a duplicate notification)

That's expected — reactors can run more than once for the same event during replay or recovery. The fix is not to prevent it but to make the reactor **idempotent**: record that the side effect happened and skip it if it already did. See [Reacting to events](/chronicle/tutorial/reacting/).

## My reactor throws and the stream seems stuck

If a reactor throws, the failing event source partition pauses until the problem is resolved — by design, so it doesn't silently skip events. Fix the underlying error (and make the reactor resilient), and processing resumes.

## A constraint is rejecting an append I expected to succeed

[Constraints](/chronicle/constraints/) are enforced as events are appended — a uniqueness constraint, for example, rejects a second event that would violate it. Check which constraint fired and whether the value really is a duplicate within its scope (remember [namespaces](/chronicle/concepts/namespaces/) isolate data per tenant).

## A projection stalls with "the schema does not declare it"

The full message names the property, the subject, and what the schema does declare:

```text
Could not apply compliance metadata for property 'address.postCode' of 'person-42'
because the schema does not declare it. The schema declares: 'street', 'city'.
```

Compliance handling walks the whole document — one `[PII]` marker anywhere switches it on for every value — and looks each property up in the schema to find the classification that applies. This message means the document and its schema have drifted apart: the stored payload carries a property the schema has never heard of. The usual cause is an event type that gained or renamed a property without an [event type migration](/chronicle/concepts/event-type-migrations/), so old events no longer match the shape the schema now describes. Add the migration and replay.

On the write side the append fails outright; on the read side the failing partition pauses and retries, so the read model stops advancing at that event.

A [geospatial](/chronicle/concepts/geospatial/) `Point`, `LineString` or `Polygon` is never the cause — the walk stops at those by design and never reads their GeoJSON members as properties.

Two shapes *are* a cause, deliberately: a polymorphic base type and a dictionary both hold members the schema does not declare, and Chronicle would rather fail here than write a value it was told is personal into the store unprotected. Model the data as a value object with declared properties. See [Geospatial values are not looked inside](/chronicle/compliance/pii#geospatial-values-are-not-looked-inside).

## I can't connect to the Chronicle kernel

- Confirm the kernel is running — if you scaffolded from a template, `docker compose up -d` and check the container is healthy.
- Confirm the client URL matches the kernel's address and port.
- Confirm your storage (MongoDB by default) is reachable from the kernel.

See [Connection strings](/chronicle/connection-strings/) and [Get started](/chronicle/get-started/).

## Events look wrong after a schema change

When an event type's shape changes over time, use [event type migrations](/chronicle/concepts/event-type-migrations/) to evolve old events to the new shape rather than mutating history. Events are immutable — you migrate how they're read, you don't edit what was written.

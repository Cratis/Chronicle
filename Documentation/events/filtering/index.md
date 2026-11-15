---
title: Filter Chronicle reducers and reactors by event metadata
description: Filter Chronicle reducers and reactors using appended event metadata so handlers receive only events that match their tags, source type or stream type.
---
Chronicle filters let a reducer or reactor handle only events with matching tags, event source type or event stream type. Put filter attributes on the observer, then supply the corresponding metadata when appending. For example, a `priority` tag can select orders for one reducer without changing the order event's payload.

## How filters correlate to append operations

| Append metadata | Observer attribute | Matches when |
| --- | --- | --- |
| `tags:` | `[FilterEventsByTag("...")]` | Any appended or static event tag matches any filter tag |
| `eventSourceType:` | `[EventSourceType("...")]` | The appended event source type matches exactly |
| `eventStreamType:` | `[EventStreamType("...")]` | The appended event stream type matches exactly |

## Important distinctions

Use `[FilterEventsByTag]`, `[EventSourceType]` or `[EventStreamType]` to control delivery to a reducer or reactor. `[Tag]` and `[Tags]` on an observer, including a projection, only label that observer; they do not filter incoming events.

Multiple `[FilterEventsByTag]` attributes accept an event with any one of the configured tags. Different filter categories work together: if you set a tag and an event source type, both categories must match. A named tag is separate from a plain string tag and is not matched by `[FilterEventsByTag]`.

## Scope

These filters select events for reducers and reactors. Projections describe what they observe through event types, joins and event sequence selection; these observer filter attributes do not narrow projection input.

Filtering delivery does not guard a write against concurrent changes. Use a [concurrency scope](../concurrency.mdx) for that.

## Guides

If the metadata is not on your events yet, start with [Appending with Tags](../appending-with-tags.mdx). Then choose the filter that matches how you categorize events:

- [Filter reducers and reactors by tag](by-tag)
- [Filter reducers and reactors by event source type](by-event-source-type)
- [Filter reducers and reactors by event stream type](by-event-stream-type)

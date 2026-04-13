# Filter observers by appended event metadata

Chronicle can filter reducers and reactors before they handle an appended event. You do this by putting filter attributes on the observer and appending matching metadata with the event.

## How filters correlate to append operations

| Append metadata | Observer attribute | Matches when |
| --- | --- | --- |
| `tags:` | `[FilterEventsByTag("...")]` | Any appended or static event tag matches any filter tag |
| `eventSourceType:` | `[EventSourceType("...")]` | The appended event source type matches exactly |
| `eventStreamType:` | `[EventStreamType("...")]` | The appended event stream type matches exactly |

## Important distinctions

- `[Tag]` and `[Tags]` on a reactor, reducer, or projection **label the observer itself**. They do not filter incoming events.
- `[FilterEventsByTag]`, `[EventSourceType]`, and `[EventStreamType]` on a reducer or reactor **filter which appended events are dispatched**.
- If you combine filter types, Chronicle applies them together. The event must satisfy every configured filter category.
- Multiple `[FilterEventsByTag]` attributes are additive. An event is dispatched when it matches any configured filter tag.

## Scope

Reducers and reactors use these filters during subscription. Projections still describe what they observe through event types, joins, and event sequence selection.

## Guides

- [Filter reducers and reactors by tag](by-tag.md)
- [Filter reducers and reactors by event source type](by-event-source-type.md)
- [Filter reducers and reactors by event stream type](by-event-stream-type.md)

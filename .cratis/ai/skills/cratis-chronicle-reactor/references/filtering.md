<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/filtering.md -->
# Filtering which events reach the reactor

This reference holds the filter attributes, how they combine and the matching append metadata.

## Filtering which events reach the reactor

| Attribute | Effect |
| --- | --- |
| `[FilterEventsByTag("<tag>")]` | **filters** by appended tag; repeatable, and any one match admits the event |
| `[EventSourceType("<value>")]` | **filters** by the appended event source type |
| `[EventStreamType("<value>")]` | **filters** by the appended event stream type |
| `[FromEventSource<TSource>("<stream>")]` | **filters** by a declared event source and stream (`Cratis.Chronicle.EventSources`) |
| `[EventStreamId("<value>")]` | **does not filter**; it is append metadata for side effects |
| `[Tag]`, `[Tags]` | **label** the reactor; they never filter |

Filter categories combine with AND; tags within the tag category combine with OR.
Typed source filters work on reactors and reducers, not projections (CHR0041).
CHR0055 flags unknown streams; CHR0056 flags routing that contradicts the filter.
Filters survive catch-up, replay and failed-partition recovery.
`WaitForCompletion` excludes nonmatching observers when the .NET client supplies
its first appended sequence number.
These correspond to metadata supplied at append time:

```csharp
await eventStore.EventLog.Append(
    <eventSourceId>,
    new <EventName>(<arguments>),
    eventStreamType: "<stream-type>",
    eventSourceType: "<source-type>",
    tags: ["<tag>"]);
```

A **projection cannot be filtered at all** — if you need metadata-based
selection, a reactor or a reducer is where it happens.

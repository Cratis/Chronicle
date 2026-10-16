<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/references/filtering.md -->
# Filtering which events reach the reducer

This reference holds the filter attributes for a reducer and the matching append metadata.

## Filtering which events reach the reducer

Place filters on the self model or separate reducer class. These attributes
genuinely filter a reducer's input, matching append metadata:

| Attribute | Filters on |
| --- | --- |
| `[FilterEventsByTag("<tag>")]` | an appended tag; repeatable, and any one match admits the event |
| `[EventSourceType("<value>")]` | the appended event source type |
| `[EventStreamType("<value>")]` | the appended event stream type |
| `[FromEventSource<TSource>("<stream>")]` | the source definition and declared stream (`Cratis.Chronicle.EventSources`) |

`[Tag]` and `[Tags]` **label** the artifact; they do not filter. Note also that a
**projection cannot be filtered at all** — its definition has no field for it.
When you need metadata-based selection, a reducer or a reactor is where it can
happen.

The matching append supplies that metadata:

```csharp
await eventStore.EventLog.Append(
    <eventSourceId>,
    new <EventName>(<arguments>),
    eventStreamType: "<stream-type>",
    eventSourceType: "<source-type>",
    tags: ["<tag>"]);
```

Mind the parameter order on `Append`: `eventStreamType` comes before
`eventStreamId`, which comes before `eventSourceType`.
CHR0055 flags an unknown typed stream and CHR0056 contradictory routing.
Filters remain effective during catch-up, replay and failed-partition recovery.

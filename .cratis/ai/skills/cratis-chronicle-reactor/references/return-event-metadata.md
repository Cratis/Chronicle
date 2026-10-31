<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/return-event-metadata.md -->
# Returned events and their metadata

This reference holds the returned-event examples, the cross-stream wrapper and where append metadata for a bare event comes from.

Requires Chronicle 19.63.0 for `[ReactorSideEffect]`
(`Cratis.Chronicle.Reactors.SideEffects`) to mark a custom synchronous return type
for CHR0054, including arrays and `IEnumerable<T>`. The marker informs the
analyzer only; a registered `IReactorSideEffectHandler` must still claim/process
the value. Check client/analyzer versions with `dotnet list package`; older
analyzers flag synchronous custom returns even when the runtime handler accepts them.

### Returning events instead of injecting `IEventLog`

Return events rather than inject `IEventLog`: **a convention, not an analyzer
contract**. Returned appends stay inside Chronicle's transaction.

```csharp
// One event, on the triggering event's own event source.
public Task<<ResultEvent>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult(new <ResultEvent>(@event.<Property>));

// Several events on that same event source.
public Task<IEnumerable<object>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult<IEnumerable<object>>([new <ResultEvent>(), new <OtherEvent>()]);
```

### Targeting another event source

`Cratis.Chronicle.EventSequences.EventForEventSourceId(eventSourceId, @event,
causation)` is the cross-stream wrapper. Its init members include `Subject`,
`Occurred`, `Tags`, `EventSource` (`Type?`) and `EventStream` (`string?`). Routing
defaults are `EventStreamType.All`, `EventStreamId.Default` and
`EventSourceType.Default`. Source: `EventSequences/EventForEventSourceId.cs`.

```csharp
public Task<IEnumerable<EventForEventSourceId>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult<IEnumerable<EventForEventSourceId>>(
    [
        new(@event.<RelatedId>, new <ResultEvent>(@event.<Property>)),
        new(@event.<OtherId>, new <OtherEvent>()) { EventStreamType = new("<stream-type>") },
    ]);
```

It also carries `IEnumerable<NamedTag> NamedTags` (snapshotted on assignment).
It does **not** carry an
event sequence id; side effects are appended to the event log.

### Where the metadata for a bare event comes from

For a **bare** event return, Chronicle resolves the append metadata from the
reactor itself: the `[EventSourceType]`, `[EventStreamType]`, and
`[EventStreamId]` attributes, and the `ICanProvideEventSourceId`,
`ICanProvideEventStreamId`, and `ICanProvideSubject` interfaces (all in
`Cratis.Chronicle.Events`, each with one method).

An `EventForEventSourceId` is **self-describing** — reactor-level metadata is not
applied to it. In a mixed collection, bare items get the reactor metadata and
wrapper items keep their own.

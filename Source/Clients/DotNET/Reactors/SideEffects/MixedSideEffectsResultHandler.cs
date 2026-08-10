// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection that mixes bare events and <see cref="EventForEventSourceId"/> returned from a reactor handler method.
/// </summary>
/// <remarks>
/// Bare events are appended to the event source id and metadata resolved from the <see cref="ReactorContext"/>
/// (the reactor's <c>[EventStreamType]</c> / <c>[EventSourceType]</c> attributes and <c>ICanProvide*</c> interfaces),
/// exactly as if returned on their own; each <see cref="EventForEventSourceId"/> keeps its own self-describing
/// metadata. All events are appended as a single transaction. Homogeneous collections are handled by
/// <see cref="EventsResultHandler"/> (all bare events) and <see cref="EventsForEventSourceIdResultHandler"/>
/// (all <see cref="EventForEventSourceId"/>); this handler covers only the mixed case.
/// </remarks>
/// <remarks>
/// The compatibility constructor retains the previous public surface for callers that create the handler directly.
/// Chronicle explicitly registers this type once per event-store scope using that constructor before convention
/// binding runs. Cached event stores use the parameterless constructor and pass their registry through the additive
/// <c>CanHandle</c> overload instead.
/// </remarks>
[Singleton]
public class MixedSideEffectsResultHandler : IReactorSideEffectHandler
{
    readonly IEventTypes? _legacyEventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MixedSideEffectsResultHandler"/> class for the current per-store contract.
    /// </summary>
    public MixedSideEffectsResultHandler()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MixedSideEffectsResultHandler"/> class for compatibility with the
    /// previous event-store-less contract.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used when the event-store-less <c>CanHandle</c> overload is
    /// called without a current event store on its <see cref="ReactorContext"/>.</param>
    public MixedSideEffectsResultHandler(IEventTypes eventTypes)
    {
        _legacyEventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        CanHandle(EventTypesFor(reactorContext), value);

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        CanHandle(eventStore.EventTypes, value);

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var eventSourceId = reactorContext.GetEventSourceId();
        var eventStreamType = reactorContext.GetEventStreamType() ?? EventStreamType.All;
        var eventStreamId = reactorContext.GetEventStreamId() ?? EventStreamId.Default;
        var eventSourceType = reactorContext.GetEventSourceType() ?? EventSourceType.Default;
        var subject = reactorContext.GetSubject();

        var events = ((IEnumerable<object>)value).Select(item => item is EventForEventSourceId eventForEventSourceId
            ? eventForEventSourceId
            : new EventForEventSourceId(eventSourceId, item)
            {
                EventStreamType = eventStreamType,
                EventStreamId = eventStreamId,
                EventSourceType = eventSourceType,
                Subject = subject
            }).ToList();

        var result = await eventStore.EventLog.AppendMany(events);

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(result, events.Select(@event => @event.EventSourceId)));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }

    static bool CanHandle(IEventTypes eventTypes, object value) =>
        value is IEnumerable<object> items &&
        items.Any() &&
        items.All(item => item is EventForEventSourceId || eventTypes.HasFor(item.GetType())) &&
        items.Any(item => item is EventForEventSourceId) &&
        items.Any(item => item is not EventForEventSourceId);

    IEventTypes EventTypesFor(ReactorContext reactorContext) =>
        reactorContext.EventStore?.EventTypes ??
        _legacyEventTypes ??
        throw new ReactorSideEffectHandlingRequiresEventStore(GetType());
}

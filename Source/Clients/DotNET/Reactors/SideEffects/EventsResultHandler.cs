// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of event objects returned from a reactor handler method.
/// Each event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
/// <remarks>
/// The compatibility constructor retains the previous public surface for callers that create the handler directly.
/// Chronicle explicitly registers this type once per event-store scope using that constructor before convention
/// binding runs. Cached event stores use the parameterless constructor and pass their registry through the additive
/// <c>CanHandle</c> overload instead.
/// </remarks>
[Singleton]
public class EventsResultHandler : IReactorSideEffectHandler
{
    readonly IEventTypes? _legacyEventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsResultHandler"/> class for the current per-store contract.
    /// </summary>
    public EventsResultHandler()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsResultHandler"/> class for compatibility with the previous
    /// event-store-less contract.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> used when the event-store-less <c>CanHandle</c> overload is
    /// called without a current event store on its <see cref="ReactorContext"/>.</param>
    public EventsResultHandler(IEventTypes eventTypes)
    {
        _legacyEventTypes = eventTypes;
    }

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> events &&
        events.All(e => EventTypesFor(reactorContext).HasFor(e.GetType()));

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        value is IEnumerable<object> events &&
        events.All(e => eventStore.EventTypes.HasFor(e.GetType()));

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var eventSourceId = reactorContext.GetEventSourceId();
        var result = await eventStore.EventLog.AppendMany(
            eventSourceId,
            (IEnumerable<object>)value,
            reactorContext.GetEventStreamType(),
            reactorContext.GetEventStreamId(),
            reactorContext.GetEventSourceType(),
            subject: reactorContext.GetSubject());

        if (result.IsSuccess)
        {
            return Result.Success<ReactorSideEffectFailure>();
        }

        return Result.Failed(ReactorSideEffectFailure.FromAppendResult(result, [eventSourceId]));
    }

    IEventTypes EventTypesFor(ReactorContext reactorContext) =>
        reactorContext.EventStore?.EventTypes ??
        _legacyEventTypes ??
        throw new ReactorSideEffectHandlingRequiresEventStore(GetType());
}

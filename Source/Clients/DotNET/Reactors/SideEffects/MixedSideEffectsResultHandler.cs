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
/// <param name="eventTypes"><see cref="IEventTypes"/> for checking whether the bare values are known event types.</param>
[Singleton]
public class MixedSideEffectsResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> items &&
        items.Any() &&
        items.All(item => item is EventForEventSourceId || eventTypes.HasFor(item.GetType())) &&
        items.Any(item => item is EventForEventSourceId) &&
        items.Any(item => item is not EventForEventSourceId);

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
            });

        var result = await eventStore.EventLog.AppendMany(events);

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(result));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}

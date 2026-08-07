// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of event objects returned from a reactor handler method.
/// Each event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
[Singleton]
public class EventsResultHandler : IReactorSideEffectHandler
{
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
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single event object returned from a reactor handler method.
/// The event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
[Singleton]
public class EventResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        eventStore.EventTypes.HasFor(value.GetType());

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var eventSourceId = reactorContext.GetEventSourceId();
        var result = await eventStore.EventLog.Append(
            eventSourceId,
            value,
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

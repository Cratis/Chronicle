// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single event object returned from a reactor handler method.
/// The event is appended to the event log using metadata resolved from the <see cref="ReactorContext"/>.
/// </summary>
/// <remarks>
/// There is deliberately no constructor taking <c>IEventTypes</c>. The one that existed was removed because the
/// registry belongs to the event store the current scope resolved, and it is not restored for compatibility: the
/// container picks the greediest constructor it can resolve and honors neither <c>[ActivatorUtilitiesConstructor]</c>
/// nor <c>[Obsolete]</c>, so a retained one would be selected over the parameterless one and would capture a scoped
/// service in a process-lifetime type all over again. <c>when_the_container_validates_scopes</c> fails if one is added.
/// </remarks>
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

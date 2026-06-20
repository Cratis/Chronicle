// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a single <see cref="EventForEventSourceId"/> returned from a reactor handler method.
/// </summary>
/// <remarks>
/// Unlike <see cref="EventResultHandler"/>, the append-metadata (event source id, stream type and id,
/// source type, subject, occurred time and causation) is taken from the <see cref="EventForEventSourceId"/>
/// itself rather than resolved from the <see cref="ReactorContext"/>. This lets a reactor explicitly target
/// an event source id other than the one of the triggering event.
/// </remarks>
[Singleton]
public class EventForEventSourceIdResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is EventForEventSourceId;

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var result = await eventStore.EventLog.AppendMany([(EventForEventSourceId)value]);

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(result));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}

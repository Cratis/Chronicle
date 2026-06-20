// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a non-empty collection of <see cref="EventForEventSourceId"/> returned from a reactor handler method.
/// </summary>
/// <remarks>
/// Matches both a statically typed <see cref="IEnumerable{T}"/> of <see cref="EventForEventSourceId"/> and an
/// <see cref="IEnumerable{T}"/> of <see cref="object"/> whose elements are all <see cref="EventForEventSourceId"/>,
/// so the events are not silently dropped when the handler method declares a less specific return type.
/// Each event carries its own append-metadata (event source id, stream type and id, source type, subject,
/// occurred time and causation), so a single return can target multiple event source ids. All events are
/// appended as one transaction. Unlike <see cref="EventsResultHandler"/>, no metadata is resolved from the
/// <see cref="ReactorContext"/>.
/// </remarks>
[Singleton]
public class EventsForEventSourceIdResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is IEnumerable<object> items && items.Any() && items.All(item => item is EventForEventSourceId);

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        var result = await eventStore.EventLog.AppendMany(((IEnumerable<object>)value).Cast<EventForEventSourceId>());

        if (!result.IsSuccess)
        {
            return Result.Failed(ReactorSideEffectFailure.FromAppendResult(result));
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}

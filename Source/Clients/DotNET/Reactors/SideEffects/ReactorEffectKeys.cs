// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Derives the idempotency keys built-in side-effect handlers put on the events a reactor returns.
/// </summary>
internal static partial class ReactorEffectKeys
{
    static readonly ConcurrentDictionary<ReactorId, byte> _reportedUnkeyed = new();

    /// <summary>
    /// Gets the key for the event at a position in the returned value, or <see langword="null"/> when it is appended unkeyed.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> of the delivery.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> events are appended to.</param>
    /// <param name="index">The zero-based position of the event in the returned value.</param>
    /// <returns>The derived key, or <see langword="null"/> when there is no delivery or the kernel lacks idempotent appends.</returns>
    internal static IdempotencyKey? ForReturned(ReactorContext reactorContext, IEventStore eventStore, int index)
    {
        if (reactorContext.Delivery is null) return null;
        if (!eventStore.EventLog.SupportsIdempotentAppends)
        {
            ReportUnkeyed(reactorContext);
            return null;
        }

        return reactorContext.Delivery.ReturnedEffectKey(index);
    }

    /// <summary>
    /// Adds the derived key, by position, to every event that does not carry one. A key a handler set itself is kept.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> of the delivery.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> events are appended to.</param>
    /// <param name="events">The events in the order the reactor returned them.</param>
    /// <returns>The events to append.</returns>
    /// <exception cref="IdempotentAppendsNotSupported">An event carries its own key and the kernel lacks idempotent appends.</exception>
    internal static IReadOnlyList<EventForEventSourceId> Apply(ReactorContext reactorContext, IEventStore eventStore, IReadOnlyList<EventForEventSourceId> events)
    {
        if (events.Any(_ => _.IdempotencyKey is not null) && !eventStore.EventLog.SupportsIdempotentAppends)
        {
            throw new IdempotentAppendsNotSupported();
        }

        if (events.All(_ => _.IdempotencyKey is not null)) return events;

        var result = new List<EventForEventSourceId>(events.Count);
        for (var index = 0; index < events.Count; index++)
        {
            var key = events[index].IdempotencyKey is null ? ForReturned(reactorContext, eventStore, index) : null;
            result.Add(key is null ? events[index] : events[index] with { IdempotencyKey = key });
        }

        return result;
    }

    [LoggerMessage(LogLevel.Warning, "Reactor '{ReactorId}' returned events that are appended without idempotency keys because the connected Chronicle kernel does not advertise idempotent appends. A recovered partition or a replay appends them again. Upgrade the kernel to make them idempotent")]
    static partial void LogUnkeyed(ILogger logger, ReactorId reactorId);

    static void ReportUnkeyed(ReactorContext reactorContext)
    {
        if (reactorContext.Logger is { } logger && _reportedUnkeyed.TryAdd(reactorContext.Delivery!.Reactor, 0))
        {
            LogUnkeyed(logger, reactorContext.Delivery.Reactor);
        }
    }
}

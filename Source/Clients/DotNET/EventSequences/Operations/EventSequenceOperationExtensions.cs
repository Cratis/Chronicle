// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Provides extension methods for <see cref="IEventSequence"/> to facilitate operations on event sequences.
/// </summary>
public static class EventSequenceOperationExtensions
{
    /// <summary>
    /// Creates an <see cref="EventSequenceOperations"/> instance for the specified <paramref name="eventSequence"/>.
    /// </summary>
    /// <param name="eventSequence">The event sequence to operate on.</param>
    /// <param name="eventSourceId"> The <see cref="EventSourceId"/> for which the operations will be configured.</param>
    /// <param name="configure">An action to configure the <see cref="EventSourceOperations"/> for a specific <see cref="EventSourceId"/>.</param>
    /// <returns>An instance of <see cref="EventSequenceOperations"/>.</returns>
    public static EventSequenceOperations ForEventSourceId(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        Action<EventSourceOperations> configure)
    {
        var builder = new EventSequenceOperations(eventSequence);
        return builder.ForEventSourceId(eventSourceId, configure);
    }
}

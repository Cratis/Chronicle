// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Decides whether an event forwarded over a federation link is accepted, using its origin event store and source sequence number.
/// </summary>
/// <remarks>
/// A link delivers in order, so a per-origin high-water mark is enough to drop redelivery. An event whose origin is the
/// receiving store is rejected so two stores consuming each other cannot loop.
/// </remarks>
/// <param name="localEventStore">The event store receiving the events.</param>
public class ForwardedEventGuard(EventStoreName localEventStore)
{
    readonly Dictionary<EventStoreName, EventSequenceNumber> _highWaterMarks = [];
    readonly object _lock = new();

    /// <summary>
    /// Evaluate a forwarded event and, when accepted, record it.
    /// </summary>
    /// <param name="origin">The event store the event originated in.</param>
    /// <param name="sourceSequenceNumber">The sequence number of the event in the origin store.</param>
    /// <returns>The decision.</returns>
    public ForwardedEventDecision Evaluate(EventStoreName origin, EventSequenceNumber sourceSequenceNumber)
    {
        if (origin == localEventStore)
        {
            return ForwardedEventDecision.RejectOwnOrigin;
        }

        lock (_lock)
        {
            if (_highWaterMarks.TryGetValue(origin, out var highWaterMark) && sourceSequenceNumber <= highWaterMark)
            {
                return ForwardedEventDecision.Duplicate;
            }

            _highWaterMarks[origin] = sourceSequenceNumber;
            return ForwardedEventDecision.Accept;
        }
    }
}

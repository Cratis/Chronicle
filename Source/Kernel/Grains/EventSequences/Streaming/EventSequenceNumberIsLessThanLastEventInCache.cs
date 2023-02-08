// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Exception that gets thrown when an event sequence number is older than the last event in the cache.
/// </summary>
public class EventSequenceNumberIsLessThanLastEventInCache : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceNumberIsLessThanLastEventInCache"/> class.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> that is older.</param>
    /// <param name="lastSequenceNumber"><see cref="EventSequenceNumber"/> of the last event.</param>
    public EventSequenceNumberIsLessThanLastEventInCache(EventSequenceNumber sequenceNumber, EventSequenceNumber lastSequenceNumber)
        : base($"Event sequence number '{sequenceNumber}' is less than the last event in the cache with sequence number '{lastSequenceNumber}'")
    {
    }
}

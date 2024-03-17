// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Exception that gets thrown when an event sequence number is older than the last event in the cache.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceNumberIsLessThanLastEventInCache"/> class.
/// </remarks>
/// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> that is older.</param>
/// <param name="lastSequenceNumber"><see cref="EventSequenceNumber"/> of the last event.</param>
public class EventSequenceNumberIsLessThanLastEventInCache(EventSequenceNumber sequenceNumber, EventSequenceNumber lastSequenceNumber)
    : Exception($"Event sequence number '{sequenceNumber}' is less than the last event in the cache with sequence number '{lastSequenceNumber}'");

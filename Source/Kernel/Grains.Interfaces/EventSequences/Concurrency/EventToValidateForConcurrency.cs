// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Represents an event to validate for concurrency.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to validate for.</param>
/// <param name="EventType">The <see cref="EventType"/> to validate for.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/>.</param>
/// <param name="EventStreamType">The <see cref="EventStreamType"/>.</param>
/// <param name="EventSourceType">The <see cref="EventSourceType"/>.</param>
/// <param name="ExpectedSequenceNumber">The expected sequence number.</param>
public record EventToValidateForConcurrency(
    EventSourceId EventSourceId,
    EventType EventType,
    EventStreamId EventStreamId,
    EventStreamType EventStreamType,
    EventSourceType EventSourceType,
    EventSequenceNumber ExpectedSequenceNumber);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents a violation of a concurrency scope.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
/// <param name="ExpectedSequenceNumber">The expected <see cref="EventSequenceNumber"/> .</param>
/// <param name="ActualSequenceNumber">The actual <see cref="EventSequenceNumber"/>.</param>
public record ConcurrencyViolation(
    EventSourceId EventSourceId,
    EventSequenceNumber ExpectedSequenceNumber,
    EventSequenceNumber ActualSequenceNumber)
{
    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of the first event that matched the scope after the expected sequence number, if it could be read.
    /// </summary>
    public EventTypeId? ConflictingEventTypeId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the first event that matched the scope after the expected sequence number, if it could be read.
    /// </summary>
    public EventSequenceNumber? ConflictingSequenceNumber { get; init; }

    /// <summary>
    /// Gets the position of the failed guard in the request, or null for single-scope operations.
    /// </summary>
    public uint? ScopeIndex { get; init; }

    /// <summary>
    /// Gets the predicate and expectation which failed.
    /// </summary>
    public Sequences.ConcurrencyScope? Scope { get; init; }
}

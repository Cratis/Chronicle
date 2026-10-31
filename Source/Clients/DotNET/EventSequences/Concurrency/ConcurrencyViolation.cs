// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency violation that occurred during an append operation.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> of the event source where the violation occurred.</param>
/// <param name="ExpectedEventSequenceNumber">The expected <see cref="EventSequenceNumber"/>.</param>
/// <param name="ActualEventSequenceNumber">The actual <see cref="EventSequenceNumber"/>.</param>
public record ConcurrencyViolation(EventSourceId EventSourceId, EventSequenceNumber ExpectedEventSequenceNumber, EventSequenceNumber ActualEventSequenceNumber)
{
    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of the first event that caused the violation, if the kernel reported it.
    /// </summary>
    /// <remarks>
    /// This is the first event matching the concurrency scope after the expected sequence number. It is null when
    /// the kernel is older than this client or could not read the event.
    /// </remarks>
    public EventTypeId? ConflictingEventTypeId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the first event that caused the violation, if the kernel reported it.
    /// </summary>
    /// <remarks>
    /// This is the first event matching the concurrency scope after the expected sequence number. It is null when
    /// the kernel is older than this client or could not read the event.
    /// </remarks>
    public EventSequenceNumber? ConflictingEventSequenceNumber { get; init; }

    /// <summary>
    /// Gets the failed scope's zero-based position in the request sent by the client, after de-duplication and resolution, when reported.
    /// </summary>
    public uint? ScopeIndex { get; init; }

    /// <summary>
    /// Gets the predicate and expected boundary that failed, when reported.
    /// </summary>
    public ConcurrencyScope? Scope { get; init; }
}

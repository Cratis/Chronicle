// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>A concurrency conflict for a protected read.</summary>
/// <param name="ReadModelType">The read model that changed.</param>
/// <param name="Key">The key that changed.</param>
public record DecisionConflict(Type ReadModelType, ReadModelKey Key)
{
    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of the first event that changed the read model after it was read, if known.
    /// </summary>
    /// <remarks>
    /// Null when the conflict was not caused by a reported concurrency violation, or the kernel did not report the event.
    /// </remarks>
    public EventTypeId? ConflictingEventTypeId { get; init; }

    /// <summary>
    /// Gets the <see cref="EventSequenceNumber"/> of the first event that changed the read model after it was read, if known.
    /// </summary>
    /// <remarks>
    /// Null when the conflict was not caused by a reported concurrency violation, or the kernel did not report the event.
    /// </remarks>
    public EventSequenceNumber? ConflictingEventSequenceNumber { get; init; }

    /// <summary>
    /// Creates a copy of this conflict carrying the conflicting event of a <see cref="ConcurrencyViolation"/>.
    /// </summary>
    /// <param name="violation">The <see cref="ConcurrencyViolation"/> that caused the conflict.</param>
    /// <returns>The conflict with the conflicting event, if the violation reported one.</returns>
    internal DecisionConflict CausedBy(ConcurrencyViolation violation) => this with
    {
        ConflictingEventTypeId = violation.ConflictingEventTypeId,
        ConflictingEventSequenceNumber = violation.ConflictingEventSequenceNumber
    };
}

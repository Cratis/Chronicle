// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// A decision read's instance (possibly absent), exact event-log watermark and projected event types.
/// </summary>
/// <typeparam name="T">The read model type.</typeparam>
/// <param name="Key">The event source key that was read.</param>
/// <param name="Instance">The instance, or null when absent.</param>
/// <param name="SequenceNumber">The last matching event, or Unavailable when no matching events exist.</param>
/// <param name="EventTypes">All types handled by the projection, including removals.</param>
public record ReadModelInstance<T>(ReadModelKey Key, T? Instance, EventSequenceNumber SequenceNumber, IReadOnlyList<EventType> EventTypes)
{
    /// <summary>
    /// Produces the exact expected concurrency scope for an append protecting this read.
    /// </summary>
    /// <returns>A scope narrowed to this event source and the projected event types.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the read is incomplete or cannot form a scope.</exception>
    public ConcurrencyScope ToConcurrencyScope()
    {
        if (!Key.IsSpecified || EventTypes.Count == 0 ||
            (!SequenceNumber.IsActualValue && !SequenceNumber.IsUnavailable))
        {
            throw new InvalidOperationException("The decision read has no exact concurrency scope.");
        }

        return new(SequenceNumber.IsUnavailable ? EventSequenceNumber.BeforeFirst : SequenceNumber, EventSourceId: Key, EventTypes: EventTypes);
    }
}

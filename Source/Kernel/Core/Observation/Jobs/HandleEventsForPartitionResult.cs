// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the result of handling events for a partition.
/// </summary>
/// <param name="LastHandledEventSequenceNumber">The sequence number of the last successfully handled event.</param>
public record HandleEventsForPartitionResult(EventSequenceNumber LastHandledEventSequenceNumber)
{
    /// <summary>
    /// Gets the sequence number of the last event the step read and dealt with, whether it handed it to the
    /// subscriber or left it out because the observer's filters exclude it.
    /// </summary>
    /// <remarks>
    /// Only set by a step that completed. Events excluded by the observer's filters are never handled, so the
    /// last handled event alone cannot say how far the step got - this is what lets the observer move past them
    /// without claiming they were handled.
    /// </remarks>
    public EventSequenceNumber LastScannedEventSequenceNumber { get; init; } = EventSequenceNumber.Unavailable;
}

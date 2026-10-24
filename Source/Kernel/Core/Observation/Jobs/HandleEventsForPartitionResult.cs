// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the result of handling events for a partition.
/// </summary>
/// <param name="LastHandledEventSequenceNumber">The sequence number of the last successfully handled event.</param>
public record HandleEventsForPartitionResult(EventSequenceNumber LastHandledEventSequenceNumber)
{
    /// <summary>
    /// Gets whether a partition replay took ownership of the remaining catch-up work.
    /// </summary>
    public bool FencedByPartitionReplay { get; init; }

    /// <summary>
    /// Gets the durable deferral when this step stopped without completing delivery.
    /// </summary>
    public PartitionDeferral? Deferral { get; init; }

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

    /// <summary>
    /// Gets the timeout that ended the step, when that is why it failed.
    /// </summary>
    /// <remarks>
    /// A timeout says the callee was too busy to answer, not that the events cannot be handled, so the step does not
    /// report the partition as failed. The job runs the step again and reports the partition only when it has run out
    /// of attempts - see <see cref="TransientTimeouts"/>.
    /// </remarks>
    public TransientPartitionFailure? TransientFailure { get; init; }
}

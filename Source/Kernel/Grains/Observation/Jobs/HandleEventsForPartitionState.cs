// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="HandleEventsForPartition"/> job step.
/// </summary>
public class HandleEventsForPartitionState : JobStepState
{
    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the event sequence id.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    public Key Partition { get; set; } = Key.Undefined;

    /// <summary>
    /// Gets or sets the next event sequence number the job step should handle.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the last handled event sequence number.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the event types to handle.
    /// </summary>
    public IEnumerable<EventType> EventTypes { get; set; } = Enumerable.Empty<EventType>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Storage.Jobs;

namespace Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="HandleEventsForPartition"/> job step.
/// </summary>
public class HandleEventsForPartitionState : JobStepState
{
    /// <summary>
    /// Gets or sets the next event sequence number the job step should handle.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or sets the last handled event sequence number.
    /// </summary>
    public EventSequenceNumber LastHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;
}

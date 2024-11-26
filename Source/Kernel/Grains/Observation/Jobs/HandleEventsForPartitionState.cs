// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the state for a <see cref="HandleEventsForPartition"/> job step.
/// </summary>
public class HandleEventsForPartitionState : JobStepState
{
    /// <summary>
    /// Gets or sets the last successfully handled event sequence number.
    /// </summary>
    public EventSequenceNumber LastSuccessfullyHandledEventSequenceNumber { get; set; } = EventSequenceNumber.Unavailable;
}

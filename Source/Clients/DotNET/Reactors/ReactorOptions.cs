// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents the options for how the client runs reactors.
/// </summary>
public class ReactorOptions
{
    int _maxConcurrentPartitions = PartitionedDeliveryScheduler.DefaultMaxConcurrentPartitions;

    /// <summary>
    /// Gets or sets how many partitions of one reactor are handled at the same time. Batches for one partition
    /// always run in arrival order. One handles partitions one at a time. Defaults to the processor count, at most eight.
    /// </summary>
    /// <exception cref="InvalidMaxConcurrentPartitions">Thrown when the value is less than one.</exception>
    public int MaxConcurrentPartitions
    {
        get => _maxConcurrentPartitions;
        set
        {
            InvalidMaxConcurrentPartitions.ThrowIfInvalid(value);
            _maxConcurrentPartitions = value;
        }
    }
}

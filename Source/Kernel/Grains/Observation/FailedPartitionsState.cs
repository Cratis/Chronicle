// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// State for a failed partition.
/// </summary>
public class FailedPartitionsState : IChildState<FailedPartitionsState>
{
    /// <summary>
    /// List of failed partitions being supervised.
    /// </summary>
    public IEnumerable<FailedPartition> FailedPartitions { get; set; } = Enumerable.Empty<FailedPartition>();
}

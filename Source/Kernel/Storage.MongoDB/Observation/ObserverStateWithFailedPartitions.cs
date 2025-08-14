// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents the state of an observer aggregated with failed partitions.
/// </summary>
public class ObserverStateWithFailedPartitions : ObserverState
{
    /// <summary>
    /// Gets or sets the set of partitions that have failed.
    /// </summary>
    public IEnumerable<FailedPartition> FailedPartitions { get; set; } = [];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Represents the number of events an observer has successfully handled for a single partition,
/// broken down by event type identifier.
/// </summary>
public class ObserverPartitionCounts
{
    /// <summary>
    /// Gets or sets the composite identifier of the counts.
    /// </summary>
    public ObserverPartitionCountsId Id { get; set; } = new(Concepts.Observation.ObserverId.Unspecified, string.Empty);

    /// <summary>
    /// Gets or sets the number of events handled for the partition, keyed by event type identifier.
    /// </summary>
    public IDictionary<string, long> Counts { get; set; } = new Dictionary<string, long>();
}

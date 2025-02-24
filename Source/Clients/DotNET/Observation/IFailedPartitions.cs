// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that can work with failed partitions.
/// </summary>
public interface IFailedPartitions
{
    /// <summary>
    /// Get any failed partitions for any observer (Reactor, Reducer ++).
    /// </summary>
    /// <returns>A collection of any <see cref="FailedPartition"/>.</returns>
    Task<IEnumerable<FailedPartition>> GetAllFailedPartitions();

    /// <summary>
    /// Get any failed partitions for a specific observer (Reactor, Reducer ++).
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns>A collection of any <see cref="FailedPartition"/>.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(ObserverId observerId);
}

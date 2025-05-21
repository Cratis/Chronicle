// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that can get information about an observer.
/// </summary>
public interface ICanGetObserverInformation
{
    /// <summary>
    /// Get the current observer state.
    /// </summary>
    /// <returns>The current <see cref="ObserverState"/>.</returns>
    Task<ObserverState> GetObserverState();

    /// <summary>
    /// Get any failed partitions for a specific reducer.
    /// </summary>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitions();
}

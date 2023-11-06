// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Persistence.Observation;

/// <summary>
/// Defines a system for working with <see cref="FailedPartition"/>.
/// </summary>
public interface IFailedPartitionsStorage
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="observerId">Optional <see cref="ObserverId"/> it is for.</param>
    /// <returns>An observable of a collection of <see cref="FailedPartition"/>.</returns>
    IObservable<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default);

    /// <summary>
    /// Save all the failed partitions.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> it is for.</param>
    /// <param name="failedPartitions"><see cref="FailedPartitions"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This will overwrite all existing failed partitions with the information in this.
    /// It will also remove any failed partitions marked as resolved.
    /// </remarks>
    Task Save(ObserverId observerId, FailedPartitions failedPartitions);

    /// <summary>
    /// Get all the failed partitions for a specific <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="FailedPartitions"/> instance.</returns>
    Task<FailedPartitions> GetFor(ObserverId observerId);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines a storage system for working with <see cref="FailedPartition"/>.
/// </summary>
public interface IFailedPartitionsStorage
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="FailedPartition"/>.
    /// </summary>
    /// <param name="observerId">Optional <see cref="ObserverId"/> it is for.</param>
    /// <returns>An observable of a collection of <see cref="FailedPartition"/>.</returns>
    ISubject<IEnumerable<FailedPartition>> ObserveAllFor(ObserverId? observerId = default);

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
    /// Remove every failed partition record for a specific <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to remove the records for.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Unlike saving an empty <see cref="FailedPartitions"/>, which only clears the partitions it is told are
    /// resolved, this removes every record keyed to the observer. That is what removing an observer needs: nothing
    /// keyed to it may survive it.
    /// </remarks>
    Task RemoveAllFor(ObserverId observerId);

    /// <summary>
    /// Get all the failed partitions for a specific <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="observerId">Optional <see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="FailedPartitions"/> instance.</returns>
    Task<FailedPartitions> GetFor(ObserverId? observerId);

    /// <summary>
    /// Get all failed partitions for a set of <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="observerIds">The <see cref="ObserverId"/> values to get for.</param>
    /// <returns><see cref="FailedPartitions"/> instance.</returns>
    Task<FailedPartitions> GetFor(IEnumerable<ObserverId> observerIds);
}

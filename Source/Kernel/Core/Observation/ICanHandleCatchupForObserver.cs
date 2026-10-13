// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that can handle catchup for a specific observer.
/// </summary>
public interface ICanHandleCatchupForObserver
{
    /// <summary>
    /// Represents an error that can occur.
    /// </summary>
    public enum Error
    {
        /// <summary>
        /// Unknown error occurred.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Handler cannot handle for the observer.
        /// </summary>
        CannotHandle = 1,
    }

    /// <summary>
    /// Begin catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> BeginCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// Resume catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> ResumeCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// End catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> EndCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// Ends this silo's bulk window at shutdown without changing observer state.
    /// </summary>
    /// <param name="observerDetails">The observer whose window is open.</param>
    /// <param name="cancellationToken">The shared stop deadline.</param>
    /// <returns>Failed partitions, including unknown write outcomes.</returns>
    Task<IEnumerable<FailedPartition>> EndBulkFor(ObserverDetails observerDetails, CancellationToken cancellationToken) =>
        Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <summary>
    /// Drain buffered writes for a progress checkpoint without ending catch-up.
    /// </summary>
    /// <param name="observerDetails">The observer to flush.</param>
    /// <returns>The open window and its buffered write failures.</returns>
    Task<BulkFlushResult> FlushFor(ObserverDetails observerDetails) => Task.FromResult(new BulkFlushResult(null, []));
}

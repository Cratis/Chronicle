// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that can handle replay for a specific observer.
/// </summary>
public interface ICanHandleReplayForObserver
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

        /// <summary>
        /// Handler could not get replay context.
        /// </summary>
        CouldNotGetReplayContext = 2,
    }

    /// <summary>
    /// Begin replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> BeginReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Resume replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> ResumeReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// End replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> EndReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Begin replay for a specific partition of an observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <param name="partition">The <see cref="Key"/> for the partition being replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition);

    /// <summary>
    /// End replay for a specific partition of an observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <param name="partition">The <see cref="Key"/> for the partition that was replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> EndReplayPartitionFor(ObserverDetails observerDetails, Key partition);
}

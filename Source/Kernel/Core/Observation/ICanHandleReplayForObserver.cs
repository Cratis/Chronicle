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
    /// Write everything held back for a replay of a specific observer, without ending the replay.
    /// </summary>
    /// <remarks>
    /// Every silo ending a replay is asked to flush before any silo is asked to end it, because ending it can make
    /// the rebuilt state the live one. A silo that ended a replay while another still held back writes for it would
    /// promote an incomplete result, and those writes would then land in a replay container nothing reads.
    /// </remarks>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> FlushReplayFor(ObserverDetails observerDetails) =>
        Task.FromResult(Result.Failed(Error.CannotHandle));

    /// <summary>
    /// Abandon a replay of a specific observer, leaving the read model as it was before the replay started.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<Error>> AbandonReplayFor(ObserverDetails observerDetails) =>
        Task.FromResult(Result.Failed(Error.CannotHandle));

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

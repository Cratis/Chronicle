// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Orleans.Services;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a service that lives in each silo and can be called to notify about replay state changes for observers.
/// </summary>
public interface IObserverService : IGrainService
{
    /// <summary>
    /// Begin replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Resume replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task ResumeReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// End replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// End replay and report whether this silo finalized the projection replay.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Whether this silo finalized the replay.</returns>
    Task<bool> TryFinalizeReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Write everything this silo holds back for a replay of a specific observer, without ending the replay.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Whether everything held back was written without failed partitions.</returns>
    Task<bool> FlushReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Abandon a replay of a specific observer without making what it rebuilt the live read model.
    /// </summary>
    /// <remarks>
    /// Used when a replay did not handle every partition: promoting its result would replace complete documents with
    /// partial or missing ones. The read model keeps the state it had before the replay started.
    /// </remarks>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task AbandonReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// Begin replay for a specific partition of an observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <param name="partition">The <see cref="Key"/> for the partition being replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition);

    /// <summary>
    /// End replay for a specific partition of an observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <param name="partition">The <see cref="Key"/> for the partition that was replayed.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplayPartitionFor(ObserverDetails observerDetails, Key partition);

    /// <summary>
    /// Begin catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// Resume catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task ResumeCatchupFor(ObserverDetails observerDetails);

    /// <summary>
    /// End catchup for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task EndCatchupFor(ObserverDetails observerDetails);
}

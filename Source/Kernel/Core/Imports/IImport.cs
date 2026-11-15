// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Defines an import session, keyed by <see cref="ImportKey"/>. It owns the import's state; staging and the commit
/// do their work elsewhere and report here.
/// </summary>
public interface IImport : IGrainWithStringKey
{
    /// <summary>
    /// Begins the import, or returns it when it was already begun for the same event sequence.
    /// </summary>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the import targets.</param>
    /// <param name="onInvalid">What the import does with invalid events.</param>
    /// <returns>The <see cref="ImportOperationResult"/>.</returns>
    Task<ImportOperationResult> Begin(EventSequenceId eventSequenceId, OnInvalidEvent onInvalid);

    /// <summary>
    /// Gets the import.
    /// </summary>
    /// <returns>The <see cref="ImportRecord"/>, or null when it was never begun.</returns>
    Task<ImportRecord?> Get();

    /// <summary>
    /// Checks that the import accepts chunks, abandoning it first when it has outlived its staging retention.
    /// </summary>
    /// <returns>The <see cref="ImportOperationResult"/>.</returns>
    Task<ImportOperationResult> EnsureAcceptsChunks();

    /// <summary>
    /// Records that a chunk was staged.
    /// </summary>
    /// <param name="summary">The <see cref="ImportChunkSummary"/>.</param>
    /// <returns>True when the chunk belongs to the import; false when the import closed without it, and the chunk must be removed.</returns>
    Task<bool> ChunkStaged(ImportChunkSummary summary);

    /// <summary>
    /// Closes staging, checks the counts the client declares and starts the commit.
    /// </summary>
    /// <param name="expectedChunks">How many chunks the client added.</param>
    /// <param name="expectedEvents">How many events the client added.</param>
    /// <returns>The <see cref="ImportOperationResult"/>.</returns>
    Task<ImportOperationResult> Complete(int expectedChunks, long expectedEvents);

    /// <summary>
    /// Abandons the import and discards its staging, when it is still staging.
    /// </summary>
    /// <param name="reason">Why the import is abandoned.</param>
    /// <returns>The <see cref="ImportOperationResult"/>.</returns>
    Task<ImportOperationResult> Abandon(string reason);

    /// <summary>
    /// Abandons the import when it has been open longer than the staging retention without activity.
    /// </summary>
    /// <returns>True when it was abandoned.</returns>
    Task<bool> AbandonIfExpired();

    /// <summary>
    /// Fails the import before anything was committed, because validation rejected events.
    /// </summary>
    /// <param name="rejections">The rejected events.</param>
    /// <returns>Awaitable task.</returns>
    Task ValidationRejected(IEnumerable<ImportRejection> rejections);

    /// <summary>
    /// Moves the import into committing.
    /// </summary>
    /// <param name="committedFrom">The sequence number from which the commit appends; nothing of the import is below it.</param>
    /// <returns>Awaitable task.</returns>
    Task CommitStarted(EventSequenceNumber committedFrom);

    /// <summary>
    /// Records the settlement of one committed chunk; this is the commit's checkpoint.
    /// </summary>
    /// <remarks>
    /// A chunk that settles after the import was failed mid-commit is recorded too, since its events are in the log; the
    /// import stays failed.
    /// </remarks>
    /// <param name="chunkIndex">The staged chunk that settled.</param>
    /// <param name="outcomes">How each of its events settled.</param>
    /// <returns>Awaitable task.</returns>
    Task ChunkCommitted(int chunkIndex, IEnumerable<ImportEventOutcome> outcomes);

    /// <summary>
    /// Completes the import once every chunk settled.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task CommitCompleted();

    /// <summary>
    /// Fails the import and discards its staging. Events it committed stay committed.
    /// </summary>
    /// <param name="reason">Why.</param>
    /// <returns>Awaitable task.</returns>
    Task Fail(string reason);

    /// <summary>
    /// Fails the import when it has been validating or committing for longer than the staging retention without
    /// progress and no commit job is left to finish it.
    /// </summary>
    /// <remarks>
    /// A commit job that is still there, running or stopped, resumes from the checkpoint, as after a kernel outage longer
    /// than the retention, so the import is left to it. When the jobs cannot be read, the import is left until the next check.
    /// </remarks>
    /// <returns>True when it was failed.</returns>
    Task<bool> FailIfStalled();

    /// <summary>
    /// Removes the import and anything left of its staging, when it ended longer ago than the staging retention.
    /// </summary>
    /// <returns>True when it was removed.</returns>
    Task<bool> RemoveIfExpired();
}

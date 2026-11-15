// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Defines an import session that events are added to in chunks, for producers that are not a single stream.
/// </summary>
/// <remarks>
/// Events are cut into chunks within the kernel's limits and a bounded number of chunks are sent at the same time.
/// Nothing is committed before <see cref="Complete"/>. Disposing a session that was not completed leaves it open on the
/// kernel, so it can be reattached by its <see cref="ImportId"/>; the kernel abandons it once its staging retention runs out.
/// </remarks>
public interface IEventImport : IAsyncDisposable
{
    /// <summary>
    /// Gets the <see cref="Imports.ImportId"/>.
    /// </summary>
    ImportId ImportId { get; }

    /// <summary>
    /// Gets how many events the kernel already held, in order and without a gap, when this session was attached. A
    /// producer that reattaches continues after this many events; a new session starts at zero.
    /// </summary>
    long EventsAcknowledged { get; }

    /// <summary>
    /// Gets the progress of the import as the kernel reports it.
    /// </summary>
    IObservable<ImportProgress> Progress { get; }

    /// <summary>
    /// Adds events to the import.
    /// </summary>
    /// <param name="events">The events, in the order they are to be committed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task, done once the events are handed to the kernel within the in-flight bound.</returns>
    Task Add(IEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds events from an asynchronous stream to the import, reading it lazily.
    /// </summary>
    /// <param name="events">The events, in the order they are to be committed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(IAsyncEnumerable<EventForEventSourceId> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the import: waits for every chunk to be acknowledged, closes staging and starts the commit.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ImportResult"/>, once committed unless <see cref="ImportOptions.WaitForCommit"/> is false.</returns>
    Task<ImportResult> Complete(CancellationToken cancellationToken = default);

    /// <summary>
    /// Abandons the import, discarding everything staged. Nothing is committed.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for waiting on chunks already being sent; when it is cancelled first, the import is not abandoned on the kernel, which abandons it once its staging retention runs out.</param>
    /// <returns>The <see cref="ImportResult"/>.</returns>
    /// <remarks>
    /// No chunk is sent once abandoning begins. An <see cref="Add(IAsyncEnumerable{EventForEventSourceId}, CancellationToken)"/>
    /// in progress is stopped rather than waited for, so a stream that never yields again cannot hold up the abandon: it
    /// ends with <see cref="ImportWasAbandoned"/> once the stream observes cancellation or yields its next event. Chunks
    /// already handed to the kernel are waited for, so none is staged after the import is abandoned. Adding to or
    /// completing the import afterwards throws <see cref="ImportWasAbandoned"/>.
    /// </remarks>
    Task<ImportResult> Abandon(CancellationToken cancellationToken = default);
}

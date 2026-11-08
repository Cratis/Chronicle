// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the progress of an import session.
/// </summary>
/// <param name="ImportId">The identifier of the import.</param>
/// <param name="EventSequenceId">The event sequence the import targets.</param>
/// <param name="State">The state of the import.</param>
/// <param name="OnInvalid">What the import does with invalid events.</param>
/// <param name="Created">When the import was begun.</param>
/// <param name="LastActivity">When the import last saw activity.</param>
/// <param name="StagedChunks">The indexes of the chunks staged and not yet discarded; a reattaching client resumes after them.</param>
/// <param name="EventsReceived">How many events the staged chunks carried.</param>
/// <param name="EventsStaged">How many events are staged.</param>
/// <param name="EventsCommitted">How many events the import committed.</param>
/// <param name="AlreadyAppended">How many events were already appended under their idempotency key.</param>
/// <param name="RejectedCount">How many events were rejected, including those beyond the retained rejections.</param>
/// <param name="Rejections">The retained rejections.</param>
/// <param name="CommittedRanges">The sequence number ranges committed so far.</param>
/// <param name="FirstSequenceNumber">The first sequence number the import committed, if any.</param>
/// <param name="LastSequenceNumber">The last sequence number the import committed, if any.</param>
/// <param name="FailureReason">Why the import failed or was abandoned, if it was.</param>
/// <param name="MaxChunkEvents">The most events one chunk may carry.</param>
/// <param name="MaxChunkBytes">The most bytes of event content one chunk may carry.</param>
/// <param name="AcknowledgedChunks">How many chunks are staged without a gap from the first; a reattached client continues with the chunk of this index.</param>
/// <param name="AcknowledgedEvents">How many events those chunks carry; a reattached client continues after this many events.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Imports)]
public record ImportProgress(
    Guid ImportId,
    string EventSequenceId,
    ImportState State,
    OnInvalidEvent OnInvalid,
    DateTimeOffset Created,
    DateTimeOffset LastActivity,
    IEnumerable<int> StagedChunks,
    long EventsReceived,
    long EventsStaged,
    long EventsCommitted,
    long AlreadyAppended,
    long RejectedCount,
    IEnumerable<ImportEventRejection> Rejections,
    IEnumerable<ImportSequenceRange> CommittedRanges,
    ulong? FirstSequenceNumber,
    ulong? LastSequenceNumber,
    string? FailureReason,
    int MaxChunkEvents,
    int MaxChunkBytes,
    int AcknowledgedChunks,
    long AcknowledgedEvents)
{
    /// <summary>
    /// Gets the progress of an import.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="importId">The identifier of the import.</param>
    /// <param name="storage">The <see cref="IStorage"/>.</param>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <returns>The <see cref="ImportProgress"/>, or null when there is no such import.</returns>
    public static Task<ImportProgress?> GetImport(EventStoreName eventStore, EventStoreNamespaceName @namespace, ImportId importId, IStorage storage, IOptions<ChronicleOptions> options) =>
        ImportProgressReader.Read(storage, eventStore, @namespace, importId, options.Value.Imports);

    /// <summary>
    /// Observes the progress of an import until it reaches a final state.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="importId">The identifier of the import.</param>
    /// <param name="storage">The <see cref="IStorage"/>.</param>
    /// <param name="options">The <see cref="ChronicleOptions"/>.</param>
    /// <returns>A subscription-owned stream of progress.</returns>
    public static ISubject<ImportProgress> ObserveImport(EventStoreName eventStore, EventStoreNamespaceName @namespace, ImportId importId, IStorage storage, IOptions<ChronicleOptions> options) =>
        ImportProgressReader.Observe(storage, eventStore, @namespace, importId, options.Value.Imports);
}

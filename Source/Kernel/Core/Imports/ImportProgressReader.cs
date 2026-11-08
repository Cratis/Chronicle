// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Reads and observes <see cref="ImportProgress"/> from storage.
/// </summary>
internal static class ImportProgressReader
{
    static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Reads the progress of an import.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/>.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="importId">The import.</param>
    /// <param name="limits">The configured import limits.</param>
    /// <returns>The progress, or null when there is no such import.</returns>
    internal static async Task<ImportProgress?> Read(IStorage storage, EventStoreName eventStore, EventStoreNamespaceName @namespace, ImportId importId, Configuration.Imports limits)
    {
        var imports = storage.GetEventStore(eventStore).GetNamespace(@namespace).Imports;
        var record = await imports.Get(importId);
        if (record is null) return null;
        var chunks = record.State.AcceptsChunks() || record.State == ImportState.Validating || record.State == ImportState.Committing
            ? await imports.GetChunkSummaries(importId)
            : [];
        return record.ToProgress(chunks, limits);
    }

    /// <summary>
    /// Observes the progress of an import, emitting when it changes, and completing once the import reaches a final state.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/>.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="importId">The import.</param>
    /// <param name="limits">The configured import limits.</param>
    /// <returns>A subscription-owned stream of progress.</returns>
    internal static ISubject<ImportProgress> Observe(IStorage storage, EventStoreName eventStore, EventStoreNamespaceName @namespace, ImportId importId, Configuration.Imports limits)
    {
        // Each subscription owns its polling loop, so the progress is the same whichever silo serves it.
        var source = Observable.Create<ImportProgress>(async (observer, cancellationToken) =>
        {
            ImportProgress? previous = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var progress = await Read(storage, eventStore, @namespace, importId, limits).WaitAsync(cancellationToken);
                if (progress is not null && !Same(previous, progress))
                {
                    observer.OnNext(progress);
                    previous = progress;
                }

                if (progress?.State.IsTerminal() == true)
                {
                    observer.OnCompleted();
                    return;
                }

                await Task.Delay(_pollInterval, cancellationToken);
            }
        });
        return Subject.Create<ImportProgress>(Observer.Create<ImportProgress>(_ => { }), source);
    }

    static bool Same(ImportProgress? previous, ImportProgress current) =>
        previous is not null &&
        previous.State == current.State &&
        previous.LastActivity == current.LastActivity &&
        previous.EventsReceived == current.EventsReceived &&
        previous.EventsCommitted == current.EventsCommitted &&
        previous.RejectedCount == current.RejectedCount;
}

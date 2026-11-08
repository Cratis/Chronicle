// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents an implementation of <see cref="IImportStager"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
[StatelessWorker]
[GrainType("chronicle-import-stager")]
public class ImportStager(IStorage storage, IOptions<ChronicleOptions> options) : Grain, IImportStager
{
    /// <inheritdoc/>
    public async Task<ImportChunkStagingResult> Stage(ImportKey key, int chunkIndex, long firstEventIndex, IReadOnlyList<ImportEvent> events, IReadOnlyList<Identity> causedBy)
    {
        if (chunkIndex < 0) return ImportChunkStagingResult.Refused(chunkIndex, "The chunk index must not be negative.");
        if (firstEventIndex < 0) return ImportChunkStagingResult.Refused(chunkIndex, "The index of the first event must not be negative.");

        var namespaceStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace);
        var existing = await namespaceStorage.Imports.GetChunk(key.ImportId, chunkIndex);
        if (existing is not null) return ImportChunkStagingResult.From(existing, true);

        var import = GrainFactory.GetGrain<IImport>(key);
        var accepts = await import.EnsureAcceptsChunks();
        if (!accepts.IsSuccess) return ImportChunkStagingResult.Refused(chunkIndex, accepts.Error!);

        var materialized = events;
        if (causedBy.Count != materialized.Count) return ImportChunkStagingResult.Refused(chunkIndex, "Every event must have the identity that caused it.");
        var limits = options.Value.Imports;
        if (materialized.Count > limits.MaxChunkEvents)
            return ImportChunkStagingResult.Refused(chunkIndex, $"The chunk carries {materialized.Count} events; the most a chunk may carry is {limits.MaxChunkEvents}.");
        var bytes = materialized.Sum(_ => (long)Encoding.UTF8.GetByteCount(_.Content));
        if (bytes > limits.MaxChunkBytes)
            return ImportChunkStagingResult.Refused(chunkIndex, $"The chunk carries {bytes} bytes of content; the most a chunk may carry is {limits.MaxChunkBytes}.");

        var eventStore = storage.GetEventStore(key.EventStore);
        var staged = new List<StagedImportEvent>();
        var rejections = new List<ImportRejection>();
        for (var position = 0; position < materialized.Count; position++)
        {
            var (stagedEvent, rejection) = await ImportEventStaging.Stage(
                materialized[position],
                firstEventIndex + position,
                causedBy[position],
                accepts.Record!.EventSequenceId,
                eventStore.EventTypes,
                eventStore.EventSources);
            if (stagedEvent is not null) staged.Add(stagedEvent);
            if (rejection is not null) rejections.Add(rejection);
        }

        var chunk = new StagedImportChunk(key.ImportId, chunkIndex, firstEventIndex, materialized.Count, staged, rejections);
        if (!await namespaceStorage.Imports.AddChunk(chunk))
        {
            // A resend raced this one and won; its staging is the one that counts.
            var winner = await namespaceStorage.Imports.GetChunk(key.ImportId, chunkIndex);
            return ImportChunkStagingResult.From(winner ?? chunk, true);
        }

        await import.ChunkStaged(chunk.ToSummary());
        return ImportChunkStagingResult.From(chunk, false);
    }
}

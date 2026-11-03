// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;
using Orleans.Storage;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling projection state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionDefinitionStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="timeProvider">The clock for first-registration metadata.</param>
public class ProjectionDefinitionStorageProvider(IStorage storage, TimeProvider timeProvider) : IGrainStorage
{
    readonly ConcurrentDictionary<(EventStoreName EventStore, ProjectionId Projection), ProjectionDefinitionDigest> _lastRecordedDigests = new();

    /// <summary>
    /// Initializes a provider using the system clock.
    /// </summary>
    /// <param name="storage">The storage.</param>
    public ProjectionDefinitionStorageProvider(IStorage storage) : this(storage, TimeProvider.System)
    {
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString());
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        return eventStore.Projections.Delete(projectionKey.ProjectionId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionDefinition>)!;
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString()!);
        var eventStore = storage.GetEventStore(projectionKey.EventStore);

        if (!await eventStore.Projections.Has(projectionKey.ProjectionId)) return;
        actualGrainState.State = await eventStore.Projections.Get(projectionKey.ProjectionId);
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ProjectionDefinition>)!;
        var projectionKey = ProjectionKey.Parse(grainId.Key.ToString()!);
        if (projectionKey.ProjectionId.IsPreview)
        {
            return;
        }
        var eventStore = storage.GetEventStore(projectionKey.EventStore);
        var definition = actualGrainState.State!;
        var cacheKey = (projectionKey.EventStore, projectionKey.ProjectionId);
        var digest = ProjectionFactory.DefinitionDigestFor(definition);

        // Registration is warm: every client connect re-registers every projection. An unchanged digest
        // needs no history round trip. A cache miss or race falls back to the storage unique-key dedup.
        if (!_lastRecordedDigests.TryGetValue(cacheKey, out var lastDigest) || lastDigest != digest)
        {
            await eventStore.ProjectionDefinitionHistory.Record(new ProjectionDefinitionHistoryEntry(
                definition.Identifier,
                digest,
                ProjectionFactory.DefinitionDigestAlgorithm,
                definition.Version,
                timeProvider.GetUtcNow(),
                definition));
            _lastRecordedDigests[cacheKey] = digest;
        }

        await eventStore.Projections.Save(definition);
    }
}

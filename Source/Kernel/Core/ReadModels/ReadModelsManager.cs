// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Orleans.Providers;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsManager"/>.
/// </summary>
/// <param name="projectionPipelines"><see cref="IProjectionPipelineManager"/> for evicting pipelines left bound to a read model's former definition.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModelsManager)]
public class ReadModelsManager(IProjectionPipelineManager projectionPipelines) : Grain<ReadModelsManagerState>, IReadModelsManager
{
    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ReadModelDefinition> definitions)
    {
        definitions = definitions.ToArray();
        foreach (var definition in definitions)
        {
            definition.Sink.EnsureReadModelSupported();
        }

        EnsureSinglePublisher(definitions);

        // A reconnecting client re-registers every read model it has, nearly always unchanged. Only definitions that
        // are new or differ by value are persisted and pushed to their read model grain; doing it for the whole set
        // on every call made registration O(read models) in Mongo writes and grain calls, and with this grain being
        // non-reentrant the stacked retries of every client could never drain.
        var readModels = State.ReadModels.ToList();
        var modified = new List<ReadModelDefinition>();
        var changed = new List<ReadModelDefinition>();
        foreach (var definition in definitions)
        {
            var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
            if (existing is not null)
            {
                if (existing.IsEquivalentTo(definition))
                {
                    continue;
                }

                readModels.Remove(existing);
                changed.Add(definition);
            }

            readModels.Add(definition);
            modified.Add(definition);
        }

        if (modified.Count > 0)
        {
            await Persist(readModels, modified);
        }

        foreach (var definition in modified)
        {
            var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
            await readModelGrain.SetDefinition(definition);
        }

        foreach (var definition in changed)
        {
            await EvictProjectionsTargeting(definition.Identifier);
        }
    }

    /// <inheritdoc/>
    public async Task RegisterSingle(ReadModelDefinition definition)
    {
        definition.Sink.EnsureReadModelSupported();
        EnsureSinglePublisher([definition]);
        var readModels = State.ReadModels.ToList();
        var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
        if (existing?.IsEquivalentTo(definition) == true)
        {
            return;
        }

        if (existing is not null)
        {
            readModels.Remove(existing);
        }

        readModels.Add(definition);
        await Persist(readModels, [definition]);

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);

        if (existing is not null)
        {
            await EvictProjectionsTargeting(definition.Identifier);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateDefinition(ReadModelDefinition definition)
    {
        definition.Sink.EnsureReadModelSupported();
        EnsureSinglePublisher([definition]);
        var readModels = State.ReadModels.ToList();
        var existing = readModels.Find(_ => _.Identifier == definition.Identifier) ?? throw new ReadModelNotFound(definition.Identifier);
        readModels.Remove(existing);
        readModels.Add(definition);
        await Persist(readModels, [definition]);

        var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
        await readModelGrain.SetDefinition(definition);

        if (!existing.IsEquivalentTo(definition))
        {
            await EvictProjectionsTargeting(definition.Identifier);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);

    async Task Persist(List<ReadModelDefinition> readModels, IEnumerable<ReadModelDefinition> modified)
    {
        // A failed write must not leave the in-memory state ahead of storage: the definition would then compare as
        // unchanged on the client's retry and never be persisted.
        var previous = State.ReadModels;
        State.ReadModels = readModels;
        State.Modified = modified.ToArray();
        try
        {
            await WriteStateAsync();
        }
        catch
        {
            State.ReadModels = previous;
            State.Modified = [];
            throw;
        }
    }

    /// <summary>
    /// Refuses definitions that would make two targets publish the same event type to the same event sequence.
    /// </summary>
    /// <param name="incoming">The <see cref="ReadModelDefinition">definitions</see> about to be stored.</param>
    /// <exception cref="EventPublisherConflict">Thrown when a destination and event type would have two publishers.</exception>
    void EnsureSinglePublisher(IEnumerable<ReadModelDefinition> incoming)
    {
        var incomingList = incoming.ToArray();
        var incomingIds = incomingList.Select(_ => _.Identifier).ToHashSet();
        var publishers = State.ReadModels.Where(_ => !incomingIds.Contains(_.Identifier)).Concat(incomingList)
            .Where(_ => _.Sink.EventSequence is not null)
            .ToArray();
        var seen = new Dictionary<(EventSequenceId Destination, EventTypeId EventType), ReadModelIdentifier>();
        foreach (var publisher in publishers)
        {
            var configuration = publisher.Sink.EventSequence!;
            var key = (configuration.Destination, EventType: configuration.EventType.Id);
            if (seen.TryGetValue(key, out var first) && first != publisher.Identifier)
            {
                throw new EventPublisherConflict(key.Destination, key.EventType, first, publisher.Identifier);
            }

            seen[key] = publisher.Identifier;
        }
    }

    /// <summary>
    /// Evicts the cached pipeline of every projection that targets a read model whose definition just
    /// changed, so the next event delivered to it rebuilds its pipeline - and the sink bound to it -
    /// from the definition just written, rather than the one in effect when the pipeline was first built.
    /// </summary>
    /// <remarks>
    /// A projection's own re-registration is a no-op whenever its field mappings are unchanged, even
    /// when the read model it targets changed underneath it - re-registering a client's unchanged
    /// projection logic every reconnect is the common case this exists to keep free, and the projection
    /// definition carries no copy of the read model's own definition to compare against. Nothing else
    /// in that path learns that this read model's definition changed, so without this call a pipeline
    /// built against a superseded container name (or any other change in the read model's definition,
    /// such as a different naming policy) keeps writing to it until the silo restarts.
    /// </remarks>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> whose definition changed.</param>
    async Task EvictProjectionsTargeting(ReadModelIdentifier readModel)
    {
        var eventStore = (EventStoreName)this.GetPrimaryKeyString();
        var projectionsManager = GrainFactory.GetGrain<IProjectionsManager>(eventStore);
        var projectionDefinitions = await projectionsManager.GetProjectionDefinitions();
        var targeting = projectionDefinitions.Where(_ => _.ReadModel == readModel).Select(_ => _.Identifier).ToArray();
        if (targeting.Length == 0)
        {
            return;
        }

        var namespaces = GrainFactory.GetGrain<INamespaces>(eventStore);
        var allNamespaces = await namespaces.GetAll();
        foreach (var @namespace in allNamespaces)
        {
            foreach (var projectionId in targeting)
            {
                projectionPipelines.EvictFor(eventStore, @namespace, projectionId);
            }
        }
    }
}

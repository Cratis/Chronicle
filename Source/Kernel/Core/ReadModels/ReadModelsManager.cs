// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.Providers;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsManager"/>.
/// </summary>
/// <param name="projectionPipelines"><see cref="IProjectionPipelineManager"/> for evicting pipelines left bound to a read model's former definition.</param>
/// <param name="sinkConfigurations">The <see cref="ISinkConfigurations"/> a named sink must be known to; none configured when omitted.</param>
/// <param name="sinkChangeReplays">The <see cref="ISinkChangeReplays"/> rebuilding a read model moved to another sink; no rebuild when omitted.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModelsManager)]
public class ReadModelsManager(
    IProjectionPipelineManager projectionPipelines,
    ISinkConfigurations? sinkConfigurations = null,
    ISinkChangeReplays? sinkChangeReplays = null) : Grain<ReadModelsManagerState>, IReadModelsManager
{
    readonly ISinkConfigurations _sinkConfigurations = sinkConfigurations ?? DefaultSinkConfigurations.Instance;
    readonly Dictionary<ReadModelIdentifier, ReadModelDefinition> _completed = new();
    readonly HashSet<ReadModelIdentifier> _pendingEvictions = new();
    readonly HashSet<ReadModelIdentifier> _pendingSinkReplays = new();

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ReadModelDefinition> definitions)
    {
        definitions = definitions.ToArray();
        foreach (var definition in definitions)
        {
            definition.Sink.EnsureReadModelSupported();
            definition.ContainerName.EnsureValid(definition.Identifier);
        }

        // Store each definition with the sink it resolves to, so a change of that sink - the read model's own, the client's
        // default or the kernel's - is a change of the stored definition. The implicit default resolves to the definition as sent.
        definitions = definitions.Select(WithResolvedSink).ToArray();

        EnsureSinglePublisher(definitions);

        // Only successfully reconciled definitions qualify for the fast path. Persisted manager state alone
        // cannot prove that SetDefinition and pipeline eviction completed during a previous attempt.
        definitions = definitions.Where(definition => !_completed.TryGetValue(definition.Identifier, out var completed) ||
            !ReadModelDefinitions.AreEqual(completed, definition)).ToArray();
        if (!definitions.Any()) return;

        var readModels = State.ReadModels.ToList();
        var modified = new List<ReadModelDefinition>();
        foreach (var definition in definitions)
        {
            _completed.Remove(definition.Identifier);
            var existing = readModels.Find(_ => _.Identifier == definition.Identifier);
            if (existing is not null && !SameSink(existing.Sink, definition.Sink))
            {
                _pendingSinkReplays.Add(definition.Identifier);
            }

            if (existing is not null)
            {
                // A fresh activation must also reconcile an equal persisted definition: the previous
                // activation may have persisted it and then failed before evicting the old pipelines.
                _pendingEvictions.Add(definition.Identifier);
                if (ReadModelDefinitions.AreEqual(existing, definition)) continue;
                readModels.Remove(existing);
            }

            readModels.Add(definition);
            modified.Add(definition);
        }

        if (modified.Count > 0)
        {
            await Persist(readModels, modified);
        }

        foreach (var definition in definitions)
        {
            var readModelGrain = GrainFactory.GetReadModel(definition.Identifier, this.GetPrimaryKeyString());
            await readModelGrain.SetDefinition(definition);
        }

        foreach (var identifier in _pendingEvictions.ToArray())
        {
            await EvictProjectionsTargeting(identifier);
            _pendingEvictions.Remove(identifier);
        }

        // The new sink is empty while the observers filling it are caught up: rebuild it, after the pipelines bound to the old sink
        // are gone and every read model grain knows the new definition.
        foreach (var identifier in _pendingSinkReplays.ToArray())
        {
            if (sinkChangeReplays is not null)
            {
                await sinkChangeReplays.ReplayFor(this.GetPrimaryKeyString(), identifier);
            }

            _pendingSinkReplays.Remove(identifier);
        }

        foreach (var definition in definitions)
        {
            _completed[definition.Identifier] = definition;
        }
    }

    /// <inheritdoc/>
    public Task RegisterSingle(ReadModelDefinition definition) => Register([definition]);

    /// <inheritdoc/>
    public async Task UpdateDefinition(ReadModelDefinition definition)
    {
        definition.Sink.EnsureReadModelSupported();
        definition = WithResolvedSink(definition);
        EnsureSinglePublisher([definition]);
        if (!State.ReadModels.Any(_ => _.Identifier == definition.Identifier)) throw new ReadModelNotFound(definition.Identifier);
        await Register([definition]);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelDefinition>> GetDefinitions() => Task.FromResult(State.ReadModels);

    /// <summary>
    /// Compares the sinks two definitions store to. A definition stored before sinks had names stores to the default sink.
    /// </summary>
    /// <param name="first">The stored <see cref="SinkDefinition"/>.</param>
    /// <param name="second">The incoming <see cref="SinkDefinition"/>.</param>
    /// <returns>True when both store to the same sink.</returns>
    static bool SameSink(SinkDefinition first, SinkDefinition second) =>
        string.Equals(first.Type.Value, second.Type.Value, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(first.Name.Value.Trim(), second.Name.Value.Trim(), StringComparison.OrdinalIgnoreCase);

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
    /// Gives a definition the sink it resolves to, refusing one naming a sink the kernel has no configuration for, or one that cannot
    /// store its kind of read model.
    /// </summary>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> about to be stored.</param>
    /// <returns>The definition with its resolved sink.</returns>
    /// <exception cref="UnknownSinkName">Thrown when the named sink is not configured.</exception>
    /// <exception cref="SinkConfigurationIsForAnotherKindOfReadModel">Thrown when the named sink cannot store the read model.</exception>
    ReadModelDefinition WithResolvedSink(ReadModelDefinition definition)
    {
        var resolved = _sinkConfigurations.ResolveFor(definition, SinkConfigurationRules.IsGraph(definition.Sink.Type));
        return resolved.IsImplicitDefault
            ? definition
            : definition with { Sink = definition.Sink with { Type = resolved.Type, Name = resolved.Name } };
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

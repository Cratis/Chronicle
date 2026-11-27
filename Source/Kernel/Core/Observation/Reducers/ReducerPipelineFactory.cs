// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineFactory"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting compliance and security fields.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the read model write configuration.</param>
/// <param name="provenance">The database provenance coordinator.</param>
/// <param name="expandoObjectConverter">The converter used to serialize committed state for watchers.</param>
/// <param name="memoryGate">Optional <see cref="ReducerMemoryGate"/> that makes reduces of large states wait for room in the memory budget.</param>
public class ReducerPipelineFactory(
    IGrainFactory grainFactory,
    IStorage storage,
    IObjectComparer objectComparer,
    IReadModelsCompliance readModelsCompliance,
    IOptions<ChronicleOptions> options,
    IReadModelSinkProvenance provenance,
    IExpandoObjectConverter? expandoObjectConverter = null,
    ReducerMemoryGate? memoryGate = null) : IReducerPipelineFactory
{
    /// <inheritdoc/>
    public async Task<IReducerPipeline> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReducerDefinition definition)
    {
        await ReadModelProducerWriteGuard.Ensure(provenance, eventStore, definition.EventSequenceId.IsSystemSequence());
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var readModel = await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(definition.ReadModel, eventStore)).GetDefinition();
        readModel.Sink.EnsureReadModelSupported();
        await provenance.EnsureReadable(eventStore, @namespace, readModel);
        var sink = await namespaceStorage.Sinks.GetFor(readModel);
        return new ReducerPipeline(
            readModel,
            sink,
            objectComparer,
            readModelsCompliance,
            eventStore,
            @namespace,
            () => provenance.EnsureReadable(eventStore, @namespace, readModel),
            options.Value.ReadModels.GuardSinkWritesOnWatermark,
            expandoObjectConverter,
            memoryGate);
    }
}

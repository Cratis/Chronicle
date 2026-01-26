// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineFactory"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
public class ReducerPipelineFactory(
    IGrainFactory grainFactory,
    IStorage storage,
    IObjectComparer objectComparer) : IReducerPipelineFactory
{
    /// <inheritdoc/>
    public async Task<IReducerPipeline> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReducerDefinition definition)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var readModel = await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(definition.ReadModel, eventStore)).GetDefinition();
        var sink = namespaceStorage.Sinks.GetFor(readModel);
        return new ReducerPipeline(readModel, sink, objectComparer);
    }
}

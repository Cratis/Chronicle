// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage;
using NJsonSchema;

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerPipelineFactory"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
public class ReducerPipelineFactory(
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
        var modelSchema = await JsonSchema.FromJsonAsync(definition.ReadModel.Schema);
        var model = new Model(definition.ReadModel.Name, modelSchema);
        var sink = namespaceStorage.Sinks.GetFor(definition.Sink.TypeId, definition.Sink.ConfigurationId, model);
        var readModel = new Model(definition.ReadModel.Name, modelSchema);
        return new ReducerPipeline(readModel, sink, objectComparer);
    }
}

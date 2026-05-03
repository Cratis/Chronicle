// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineFactory"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="complianceManager">The <see cref="IJsonComplianceManager"/> for encrypting and decrypting PII fields.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between ExpandoObject and JsonObject.</param>
public class ReducerPipelineFactory(
    IGrainFactory grainFactory,
    IStorage storage,
    IObjectComparer objectComparer,
    IJsonComplianceManager complianceManager,
    IExpandoObjectConverter expandoObjectConverter) : IReducerPipelineFactory
{
    /// <inheritdoc/>
    public async Task<IReducerPipeline> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReducerDefinition definition)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var readModel = await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(definition.ReadModel, eventStore)).GetDefinition();
        var sink = await namespaceStorage.Sinks.GetFor(readModel);
        return new ReducerPipeline(readModel, sink, objectComparer, complianceManager, expandoObjectConverter, eventStore, @namespace);
    }
}

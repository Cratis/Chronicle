// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
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
public class ReducerPipelineFactory(
    IGrainFactory grainFactory,
    IStorage storage,
    IObjectComparer objectComparer,
    IReadModelsCompliance readModelsCompliance,
    IOptions<ChronicleOptions> options) : IReducerPipelineFactory
{
    /// <inheritdoc/>
    public Task<IReducerPipeline> Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerDefinition definition) =>
        CreatePipeline(eventStore, @namespace, definition, null);

    /// <inheritdoc/>
    public Task<IReducerPipeline> CreateForReplay(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerDefinition definition, ReplayContext context) =>
        CreatePipeline(eventStore, @namespace, definition, context);

    async Task<IReducerPipeline> CreatePipeline(EventStoreName eventStore, EventStoreNamespaceName @namespace, ReducerDefinition definition, ReplayContext? context)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var readModel = await grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(definition.ReadModel, eventStore)).GetDefinition();
        if (context is not null)
        {
            if (context.ReplayContainerName is null || context.Type.Identifier != readModel.Identifier)
            {
                throw new ReplayInitializationFailed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext);
            }

            // Sinks are cached by container name. No BeginReplay/ResumeReplay call changes the live sink,
            // and a late reply belonging to a removed job can only change its own obsolete target.
            readModel = readModel with { ContainerName = context.ReplayContainerName };
        }

        var sink = await namespaceStorage.Sinks.GetFor(readModel);
        return new ReducerPipeline(
            readModel,
            sink,
            objectComparer,
            readModelsCompliance,
            eventStore,
            @namespace,
            options.Value.ReadModels.GuardSinkWritesOnWatermark);
    }
}

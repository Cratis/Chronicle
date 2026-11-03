// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IPartitionRebuildAdmission"/>.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the feature flags.</param>
/// <param name="storage">The <see cref="IStorage"/> for reading definitions, sinks and generation state.</param>
/// <param name="services">The <see cref="IServiceProvider"/> for the generation bindings, which not every storage provider registers.</param>
/// <param name="logger">The logger.</param>
public class PartitionRebuildAdmission(
    IOptions<ChronicleOptions> options,
    IStorage storage,
    IServiceProvider services,
    ILogger<PartitionRebuildAdmission> logger) : IPartitionRebuildAdmission
{
    /// <inheritdoc/>
    public async Task<bool> IsAdmitted(ReplayObserverPartitionRequest request)
    {
        var features = options.Value.Features;
        if (!features.IsolatedPartitionReplay || request.ObserverType != ObserverType.Projection)
        {
            return false;
        }

        try
        {
            var candidate = await DescribeCandidate(request);
            return PartitionRebuildEligibility.IsEligible(features, candidate);
        }
        catch (Exception exception)
        {
            logger.CouldNotDecideIsolatedRebuild(request.ObserverKey.ObserverId, request.Key, exception);
            return false;
        }
    }

    static async Task<bool> HasUnchangedGenerationState(IEventStoreStorage eventStore, GenerationFloor? floor)
    {
        var current = await eventStore.GenerationFloor.Get();
        return current?.Incarnation == floor?.Incarnation && current?.Revision == floor?.Revision &&
            (await eventStore.GenerationInitialization.GetReceipts()).Count == 0;
    }

    async Task<PartitionRebuildCandidate> DescribeCandidate(ReplayObserverPartitionRequest request)
    {
        var key = request.ObserverKey;
        var eventStore = storage.GetEventStore(key.EventStore);
        ProjectionDefinition? definition = null;
        var sinkSupportsIsolatedReplace = false;
        if (await eventStore.Projections.Has(key.ObserverId))
        {
            definition = await eventStore.Projections.Get(key.ObserverId);
            var readModel = await eventStore.ReadModels.Get(definition.ReadModel);
            var sink = await eventStore.GetNamespace(key.Namespace).Sinks.GetFor(readModel);
            sinkSupportsIsolatedReplace = sink.SupportsIsolatedReplace;
        }

        return new(
            request.ObserverType,
            definition,
            request.ReplaysAllEventTypes,
            sinkSupportsIsolatedReplace,
            await IsUntouchedByGenerations(eventStore, key));
    }

    async Task<bool> IsUntouchedByGenerations(IEventStoreStorage eventStore, ObserverKey key)
    {
        if ((await eventStore.GenerationInitialization.GetReceipts()).Count > 0)
        {
            return false;
        }

        var floor = await eventStore.GenerationFloor.Get();
        if (floor is null || floor.Incarnation == StoreIncarnation.Unspecified)
        {
            return await HasUnchangedGenerationState(eventStore, floor);
        }

        var keys = services.GetService<IMaterializationKeys>();
        var bindings = services.GetService<IMaterializationBindingStorage>();
        if (keys is null || bindings is null)
        {
            return false;
        }

        var materializationKey = await keys.For(key.EventStore, key.Namespace, key.ObserverId);
        return await bindings.Get(key.EventStore, materializationKey) is null &&
            await HasUnchangedGenerationState(eventStore, floor);
    }
}

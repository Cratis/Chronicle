// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Projections;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Utilities;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projection"/> class.
/// </remarks>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionManager"><see cref="IProjectionManager"/> for managing projections.</param>
/// <param name="projectionDefinitionComparer"><see cref="IProjectionDefinitionComparer"/> for comparing projection definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class Projection(
    IProjectionFactory projectionFactory,
    IProjectionManager projectionManager,
    IProjectionDefinitionComparer projectionDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    ILogger<Projection> logger) : Grain<ProjectionDefinition>, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromMinutes(1), logger);
    IObserver? _observer;
    bool _subscribed;

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ProjectionDefinition definition)
    {
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var compareResult = await projectionDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

        if (compareResult == ProjectionDefinitionCompareResult.Different)
        {
            if (_subscribed)
            {
                await _observer!.Unsubscribe();
                _subscribed = false;
            }
            _definitionObservers.Notify(_ => _.OnProjectionDefinitionsChanged());
            var namespaceNames = await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll();
            await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
        }

        if (!_subscribed && definition.IsActive)
        {
            _observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(key.ProjectionId, key.EventStore, key.Namespace, key.EventSequenceId));
            var projection = await projectionFactory.Create(key.EventStore, key.Namespace, definition);
            projectionManager.Register(key.EventStore, key.Namespace, projection);

            await _observer.Subscribe<IProjectionObserverSubscriber>(
                ObserverType.Projection,
                projection.EventTypes,
                localSiloDetails.SiloAddress);

            _subscribed = true;
        }
    }

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    async Task AddReplayRecommendationForAllNamespaces(ProjectionKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Projection definition has changed.",
                new ReplayCandidateRequest
                {
                    ObserverId = key.ProjectionId,
                    ObserverKey = new ObserverKey(key.ProjectionId, key.EventStore, @namespace, key.EventSequenceId),
                    Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}

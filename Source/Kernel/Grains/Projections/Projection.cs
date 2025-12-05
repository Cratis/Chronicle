// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
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
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="projectionDefinitionComparer"><see cref="IProjectionDefinitionComparer"/> for comparing projection definitions.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class Projection(
    IStorage storage,
    IProjectionDefinitionComparer projectionDefinitionComparer,
    ILogger<Projection> logger) : Grain<ProjectionDefinition>, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromDays(365 * 4), logger);

    /// <inheritdoc/>
    public async Task SetDefinition(ProjectionDefinition definition)
    {
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
        logger.SettingDefinition(key.ProjectionId);
        var compareResult = await projectionDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

        if (compareResult == ProjectionDefinitionCompareResult.Different)
        {
            logger.ProjectionHasChanged(key.ProjectionId);
            await _definitionObservers.Notify(notifier => notifier.OnProjectionDefinitionsChanged(definition));
            var namespaceNames = await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll();
            await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
        }
    }

    /// <inheritdoc/>
    public Task<ProjectionDefinition> GetDefinition() => Task.FromResult(State);

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Unsubscribe(subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionFuture>> GetFutures(EventStoreNamespaceName @namespace)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var futuresStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(@namespace).ProjectionFutures;
        return futuresStorage.GetForProjection(projectionKey.ProjectionId);
    }

    /// <inheritdoc/>
    public async Task AddFuture(EventStoreNamespaceName @namespace, ProjectionFuture future)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var futuresStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(@namespace).ProjectionFutures;
        await futuresStorage.Save(projectionKey.ProjectionId, future);
    }

    /// <inheritdoc/>
    public async Task ResolveFuture(EventStoreNamespaceName @namespace, ProjectionFutureId futureId)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var futuresStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(@namespace).ProjectionFutures;
        await futuresStorage.Remove(projectionKey.ProjectionId, futureId);
    }

    async Task AddReplayRecommendationForAllNamespaces(ProjectionKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Projection definition has changed.",
                new()
                {
                    ObserverId = key.ProjectionId,
                    ObserverKey = new(key.ProjectionId, key.EventStore, @namespace, State.EventSequenceId),
                    Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}

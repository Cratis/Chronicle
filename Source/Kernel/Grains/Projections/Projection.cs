// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Utilities;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Projection"/> class.
/// </remarks>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class Projection(
    IProjectionFactory projectionFactory,
    ILocalSiloDetails localSiloDetails,
    ILogger<Projection> logger) : Grain<ProjectionDefinition>, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromMinutes(1), logger);
    IObserver? _observer;

    /// <inheritdoc/>
    public async Task SetDefinition(ProjectionDefinition definition)
    {
        State = definition;
        await WriteStateAsync();

        // TODO: Compare for changes, if it has changed notify any observers and add a recommendation for replay
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());

        _observer = GrainFactory.GetGrain<IObserver>(Guid.Parse(key.ProjectionId), new ObserverKey(key.EventStore, key.Namespace, key.EventSequenceId));
        var projection = await projectionFactory.CreateFrom(definition);

        await _observer.Subscribe<IProjectionObserverSubscriber>(
            State.Name.Value,
            ObserverType.Projection,
            projection.EventTypes,
            localSiloDetails.SiloAddress);

        _definitionObservers.Notify(_ => _.OnProjectionDefinitionsChanged());
    }

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    // async Task AddReplayRecommendationForAllNamespaces(ProjectionId projectionId, IEnumerable<EventStoreNamespaceName> namespaces)
    // {
    //     foreach (var @namespace in namespaces)
    //     {
    //         var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(_eventStoreName, @namespace));
    //         await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
    //             "Projection definition has changed.",
    //             new ReplayCandidateRequest
    //             {
    //                 ObserverId = (Guid)projectionId,
    //                 ObserverKey = new ObserverKey(_eventStoreName, @namespace, EventSequenceId.Log),
    //                 Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
    //             });
    //     }
    // }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Replaying;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
[ImplicitChannelSubscription]
[StatelessWorker(1)]
public class Projections : Grain, IProjections, IOnBroadcastChannelSubscribed
{
    readonly IKernel _kernel;
    readonly IClusterClient _clusterClient;
    readonly ILogger<Projections> _logger;
    readonly IBroadcastChannelProvider _projectionChangedChannel;
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> instance.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Projections(
        IKernel kernel,
        IClusterClient clusterClient,
        ILogger<Projections> logger)
    {
        _kernel = kernel;
        _clusterClient = clusterClient;
        _logger = logger;
        _projectionChangedChannel = _clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ProjectionChanged);
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.Rehydrate();

        _eventStoreName = this.GetPrimaryKeyString();
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var eventStore = _kernel.GetEventStore(_eventStoreName);
        var projectionDefinitions = await eventStore.ProjectionDefinitions.GetAll();
        foreach (var namespaceName in namespaces)
        {
            await EnsureProjectionPipelinesForNamespace(projectionDefinitions, namespaceName);
        }
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionAndPipeline> registrations)
    {
        var eventStoreInstance = _kernel.GetEventStore(_eventStoreName);

        var namespaceNames = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var namespaces = namespaceNames.Select(eventStoreInstance.GetNamespace);

        foreach (var registration in registrations)
        {
            var projectionDefinition = registration.Projection;
            var pipelineDefinition = registration.Pipeline;

            var (isNew, hasChanged) = await eventStoreInstance.ProjectionDefinitions.IsNewOrChanged(projectionDefinition);
            if (isNew)
            {
                _logger.ProjectionIsNew(projectionDefinition.Identifier, projectionDefinition.Name);
            }

            if (hasChanged || isNew)
            {
                await RegisterProjectionAndPipeline(
                    projectionDefinition,
                    pipelineDefinition);
            }

            if (hasChanged)
            {
                _logger.ProjectionHasChanged(projectionDefinition.Identifier, projectionDefinition.Name);
                await AddReplayRecommendationForAllNamespaces(projectionDefinition.Identifier, namespaceNames);
            }

            foreach (var @namespace in namespaces.Where(_ => !_.ProjectionManager.Exists(projectionDefinition.Identifier)))
            {
                await EnsureProjectionPipeline(projectionDefinition, @namespace.Name);
            }
        }
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);

        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        var eventStore = _kernel.GetEventStore(_eventStoreName);
        var projectionDefinitions = await eventStore.ProjectionDefinitions.GetAll();
        await EnsureProjectionPipelinesForNamespace(projectionDefinitions, added.Namespace);
    }

    Task OnError(Exception exception)
    {
        return Task.CompletedTask;
    }

    async Task AddReplayRecommendationForAllNamespaces(ProjectionId projectionId, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(_eventStoreName, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Projection definition has changed.",
                new ReplayCandidateRequest
                {
                    ObserverId = (Guid)projectionId,
                    ObserverKey = new ObserverKey(_eventStoreName, @namespace, EventSequenceId.Log),
                    Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
                });
        }
    }

    async Task EnsureProjectionPipelinesForNamespace(IEnumerable<ProjectionDefinition> projectionDefinitions, EventStoreNamespaceName namespaceName)
    {
        foreach (var projectionDefinition in projectionDefinitions)
        {
            await EnsureProjectionPipeline(projectionDefinition, namespaceName);
        }
    }

    async Task EnsureProjectionPipeline(
        ProjectionDefinition projectionDefinition,
        EventStoreNamespaceName @namespace)
    {
        var key = new ProjectionKey(_eventStoreName, @namespace, EventSequenceId.Log);
        var projection = GrainFactory.GetGrain<IProjection>(projectionDefinition.Identifier, key);
        await projection.Ensure();
        await projection.RefreshDefinition();
    }

    async Task RegisterProjectionAndPipeline(
        ProjectionDefinition projectionDefinition,
        ProjectionPipelineDefinition pipelineDefinition)
    {
        _logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);

        var eventStore = _kernel.GetEventStore(_eventStoreName);
        await eventStore.ProjectionDefinitions.Register(projectionDefinition);
        await eventStore.ProjectionPipelineDefinitions.Register(pipelineDefinition);
    }
}

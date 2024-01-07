// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Grains.Observation.States;
using Aksio.Cratis.Kernel.Grains.Recommendations;
using Aksio.Cratis.Kernel.Observation.Replaying;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Concurrency;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
[ImplicitChannelSubscription]
[StatelessWorker(1)]
public class Projections : Grain, IProjections, IOnBroadcastChannelSubscribed
{
    readonly IKernel _kernel;
    readonly IClusterClient _clusterClient;
    readonly KernelConfiguration _configuration;
    readonly Microservices _microservices;
    readonly ILogger<Projections> _logger;
    readonly IBroadcastChannelProvider _projectionChangedChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> instance.</param>
    /// <param name="configuration">The Kernel configuration.</param>
    /// <param name="microservices">All configured microservices.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Projections(
        IKernel kernel,
        IClusterClient clusterClient,
        KernelConfiguration configuration,
        Microservices microservices,
        ILogger<Projections> logger)
    {
        _kernel = kernel;
        _clusterClient = clusterClient;
        _configuration = configuration;
        _microservices = microservices;
        _logger = logger;
        _projectionChangedChannel = _clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ProjectionChanged);
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        _logger.Rehydrate();

        foreach (var microserviceId in _microservices.GetMicroserviceIds())
        {
            var eventStore = _kernel.GetEventStore((string)microserviceId);
            var projectionPipelineDefinitions = await eventStore.ProjectionPipelineDefinitions.GetAll();
            foreach (var pipeline in projectionPipelineDefinitions)
            {
                if (await eventStore.ProjectionDefinitions.TryGetFor(pipeline.ProjectionId) is (true, ProjectionDefinition projectionDefinition))
                {
                    if (!projectionDefinition.IsActive) continue;

                    foreach (var tenant in _configuration.Tenants.GetTenantIds())
                    {
                        var eventStoreNamespace = eventStore.GetNamespace(tenant);
                        await eventStoreNamespace.ProjectionManager.Register(projectionDefinition, pipeline);
                        var key = new ProjectionKey(microserviceId, tenant, EventSequenceId.Log);
                        await GrainFactory.GetGrain<IProjection>(pipeline.ProjectionId, key).Ensure();
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task Register(MicroserviceId microserviceId, IEnumerable<ProjectionAndPipeline> registrations)
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ProjectionChanged, Guid.Empty);
        var channelWriter = _projectionChangedChannel.GetChannelWriter<ProjectionChanged>(channelId);
        var eventStore = _kernel.GetEventStore((string)microserviceId);

        foreach (var registration in registrations)
        {
            var projectionDefinition = registration.Projection;
            var pipelineDefinition = registration.Pipeline;

            var (isNew, hasChanged) = await eventStore.ProjectionDefinitions.IsNewOrChanged(projectionDefinition);
            var existsInAllNamespaces = eventStore.Namespaces.All(_ => _.ProjectionManager.Exists(projectionDefinition.Identifier));

            if (hasChanged || isNew || existsInAllNamespaces)
            {
                await RegisterProjectionAndPipeline(
                    microserviceId,
                    projectionDefinition,
                    pipelineDefinition,
                    isNew);

                await channelWriter.Publish(
                    new ProjectionChanged(
                        RuntimeIdentity,
                        microserviceId,
                        projectionDefinition,
                        pipelineDefinition,
                        isNew));
            }
        }
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription) => streamSubscription.Attach<ProjectionChanged>(OnProjectionChanged, OnError);

    async Task OnProjectionChanged(ProjectionChanged changed)
    {
        if (changed.RuntimeIdentity == RuntimeIdentity) return;

        await RegisterProjectionAndPipeline(
            changed.MicroserviceId,
            changed.Projection,
            changed.Pipeline,
            changed.IsNew);
    }

    Task OnError(Exception exception)
    {
        return Task.CompletedTask;
    }

    async Task RegisterProjectionAndPipeline(
        MicroserviceId microserviceId,
        ProjectionDefinition projectionDefinition,
        ProjectionPipelineDefinition pipelineDefinition,
        bool isNew)
    {
        _logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);

        var eventStore = _kernel.GetEventStore((string)microserviceId);
        await eventStore.ProjectionDefinitions.Register(projectionDefinition);
        await eventStore.ProjectionPipelineDefinitions.Register(pipelineDefinition);

        foreach (var tenant in _configuration.Tenants.GetTenantIds())
        {
            var eventStoreNamespace = eventStore.GetNamespace(tenant);
            await eventStoreNamespace.ProjectionManager.Register(projectionDefinition, pipelineDefinition);

            if (!projectionDefinition.IsActive) continue;

            var key = new ProjectionKey(microserviceId, tenant, EventSequenceId.Log);
            var projection = GrainFactory.GetGrain<IProjection>(projectionDefinition.Identifier, key);
            await projection.Ensure();
            await projection.RefreshDefinition();
            if (isNew)
            {
                _logger.ProjectionIsNew(projectionDefinition.Identifier, projectionDefinition.Name);
            }
            else
            {
                _logger.ProjectionHasChanged(projectionDefinition.Identifier, projectionDefinition.Name);

                foreach (var tenantId in _configuration.Tenants.GetTenantIds())
                {
                    var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(microserviceId, tenantId));
                    await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                        "Projection definition has changed.",
                        new ReplayCandidateRequest
                        {
                            ObserverId = (Guid)projectionDefinition.Identifier,
                            ObserverKey = new ObserverKey(microserviceId, tenantId, EventSequenceId.Log),
                            Reasons = new[]
                            {
                                new ProjectionDefinitionChangedReplayCandidateReason()
                            }
                        });
                }
            }
        }
    }
}

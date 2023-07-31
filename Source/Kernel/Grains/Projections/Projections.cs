// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.DependencyInversion;
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
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitions;
    readonly ProviderFor<IProjectionPipelineDefinitions> _projectionPipelineDefinitions;
    readonly ProviderFor<IProjectionManager> _projectionManager;
    readonly IExecutionContextManager _executionContextManager;
    readonly IClusterClient _clusterClient;
    readonly Tenants _tenants;
    readonly Microservices _microservices;
    readonly ILogger<Projections> _logger;
    readonly IBroadcastChannelProvider _projectionChangedChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="projectionDefinitions">Provider for the <see cref="IProjectionDefinitions"/> to use.</param>
    /// <param name="projectionPipelineDefinitions">Provider for the <see cref="IProjectionPipelineDefinitions"/> to use.</param>
    /// <param name="projectionManager">Provider for the <see cref="IProjectionManager"/> to use.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> instance.</param>
    /// <param name="tenants">All configured tenants.</param>
    /// <param name="microservices">All configured microservices.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Projections(
        ProviderFor<IProjectionDefinitions> projectionDefinitions,
        ProviderFor<IProjectionPipelineDefinitions> projectionPipelineDefinitions,
        ProviderFor<IProjectionManager> projectionManager,
        IExecutionContextManager executionContextManager,
        IClusterClient clusterClient,
        Tenants tenants,
        Microservices microservices,
        ILogger<Projections> logger)
    {
        _projectionDefinitions = projectionDefinitions;
        _projectionPipelineDefinitions = projectionPipelineDefinitions;
        _projectionManager = projectionManager;
        _executionContextManager = executionContextManager;
        _clusterClient = clusterClient;
        _tenants = tenants;
        _microservices = microservices;
        _logger = logger;
        _projectionChangedChannel = _clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ProjectionChanged);
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        foreach (var microserviceId in _microservices.GetMicroserviceIds())
        {
            var projectionPipelineDefinitions = await _projectionPipelineDefinitions().GetAll();
            foreach (var pipeline in projectionPipelineDefinitions)
            {
                _executionContextManager.Establish(microserviceId);
                if (await _projectionDefinitions().HasFor(pipeline.ProjectionId))
                {
                    foreach (var tenant in _tenants.GetTenantIds())
                    {
                        _executionContextManager.Establish(tenant, CorrelationId.New(), microserviceId);
                        var projectionDefinition = await _projectionDefinitions().GetFor(pipeline.ProjectionId);
                        await _projectionManager().Register(projectionDefinition, pipeline);

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
        _executionContextManager.Establish(microserviceId);

        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ProjectionChanged, Guid.Empty);
        var channelWriter = _projectionChangedChannel.GetChannelWriter<ProjectionChanged>(channelId);

        foreach (var registration in registrations)
        {
            var projectionDefinition = registration.Projection;
            var pipelineDefinition = registration.Pipeline;

            var isNew = !await _projectionDefinitions().HasFor(projectionDefinition.Identifier);
            var hasChanged = await _projectionDefinitions().HasChanged(projectionDefinition);

            if (hasChanged || isNew)
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
        _executionContextManager.Establish(microserviceId);
        _logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);

        await _projectionDefinitions().Register(projectionDefinition);
        await _projectionPipelineDefinitions().Register(pipelineDefinition);

        foreach (var tenant in _tenants.GetTenantIds())
        {
            _executionContextManager.Establish(tenant, CorrelationId.New(), microserviceId);
            await _projectionManager().Register(projectionDefinition, pipelineDefinition);

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
                await projection.Rewind();
            }
        }
    }
}

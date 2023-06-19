// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjections"/>.
/// </summary>
public class Projections : Grain, IProjections
{
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitions;
    readonly ProviderFor<IProjectionPipelineDefinitions> _projectionPipelineDefinitions;
    readonly IExecutionContextManager _executionContextManager;
    readonly Tenants _tenants;
    readonly ILogger<Projections> _logger;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projections"/> class.
    /// </summary>
    /// <param name="projectionDefinitions">Provider for the <see cref="IProjectionDefinitions"/> to use.</param>
    /// <param name="projectionPipelineDefinitions">Provider for the <see cref="IProjectionPipelineDefinitions"/> to use.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="tenants">All configured tenants.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public Projections(
        ProviderFor<IProjectionDefinitions> projectionDefinitions,
        ProviderFor<IProjectionPipelineDefinitions> projectionPipelineDefinitions,
        IExecutionContextManager executionContextManager,
        Tenants tenants,
        ILogger<Projections> logger)
    {
        _projectionDefinitions = projectionDefinitions;
        _projectionPipelineDefinitions = projectionPipelineDefinitions;
        _executionContextManager = executionContextManager;
        _tenants = tenants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _microserviceId = this.GetPrimaryKey();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        _executionContextManager.Establish(_microserviceId);
        var projectionPipelineDefinitions = await _projectionPipelineDefinitions().GetAll();
        foreach (var pipeline in projectionPipelineDefinitions)
        {
            if (await _projectionDefinitions().HasFor(pipeline.ProjectionId))
            {
                foreach (var tenant in _tenants.GetTenantIds())
                {
                    var key = new ProjectionKey(_microserviceId, tenant, EventSequenceId.Log);
                    await GrainFactory.GetGrain<IProjection>(pipeline.ProjectionId, key).Ensure();
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionAndPipeline> registrations)
    {
        foreach (var registration in registrations)
        {
            var projectionDefinition = registration.Projection;
            var pipelineDefinition = registration.Pipeline;

            _logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);
            var projectionDefinitions = _projectionDefinitions();
            var projectionPipelineDefinitions = _projectionPipelineDefinitions();

            var isNew = !await projectionDefinitions.HasFor(projectionDefinition.Identifier);
            var hasChanged = await projectionDefinitions.HasChanged(projectionDefinition);

            if (hasChanged || isNew)
            {
                await projectionDefinitions.Register(projectionDefinition);
                await projectionPipelineDefinitions.Register(pipelineDefinition);

                foreach (var tenant in _tenants.GetTenantIds())
                {
                    var key = new ProjectionKey(_microserviceId, tenant, EventSequenceId.Log);
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
    }
}

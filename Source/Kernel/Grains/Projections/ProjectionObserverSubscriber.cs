// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.DependencyInversion;
using Orleans;
using EngineProjection = Aksio.Cratis.Kernel.Engines.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
public class ProjectionObserverSubscriber : Grain, IProjectionObserverSubscriber, INotifyProjectionDefinitionsChanged
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly ProviderFor<IProjectionPipelineDefinitions> _projectionPipelineDefinitionsProvider;
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    ProjectionId _projectionId;
    ProjectionDefinition? _definition;
    ProjectionPipelineDefinition? _pipelineDefinition;
    EngineProjection? _projection;
    IProjectionPipeline? _pipeline;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="projectionDefinitionsProvider"><see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionPipelineDefinitionsProvider"><see cref="IProjectionPipelineDefinitions"/> for working with pipelines.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> for creating the pipeline for the projection.</param>
    public ProjectionObserverSubscriber(
        IExecutionContextManager executionContextManager,
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        ProviderFor<IProjectionPipelineDefinitions> projectionPipelineDefinitionsProvider,
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory)
    {
        _executionContextManager = executionContextManager;
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
        _projectionPipelineDefinitionsProvider = projectionPipelineDefinitionsProvider;
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
        _projectionId = ProjectionId.NotSet;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;
        var projection = GrainFactory.GetGrain<IProjection>(_projectionId, new ProjectionKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));
        await projection.SubscribeDefinitionsChanged(this);

        await HandleDefinitionsAndInstances();
    }

    /// <inheritdoc/>
    public void OnProjectionDefinitionsChanged()
    {
        Task.Run(HandleDefinitionsAndInstances);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(AppendedEvent @event, ObserverSubscriberContext context)
    {
        if (_pipeline is null)
        {
            return ObserverSubscriberResult.Disconnected;
        }

        try
        {
            _executionContextManager.Establish(_tenantId, @event.Context.CorrelationId, _microserviceId);
            await _pipeline.Handle(@event);
            return ObserverSubscriberResult.Ok;
        }
        catch (Exception ex)
        {
            return new(ObserverSubscriberState.Failed, ex.GetAllMessages(), ex.StackTrace ?? string.Empty);
        }
    }

    async Task HandleDefinitionsAndInstances()
    {
        _executionContextManager.Establish(_tenantId, CorrelationId.New(), _microserviceId);

        _definition = await _projectionDefinitionsProvider().GetFor(_projectionId);
        _pipelineDefinition = await _projectionPipelineDefinitionsProvider().GetFor(_projectionId);
        _projection = await _projectionFactory.CreateFrom(_definition);
        _pipeline = _projectionPipelineFactory.CreateFrom(_projection, _pipelineDefinition);
    }
}

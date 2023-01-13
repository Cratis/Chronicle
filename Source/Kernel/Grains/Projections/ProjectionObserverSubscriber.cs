// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Kernel.Engines.Projections.Definitions;
using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Orleans;
using EngineProjection = Aksio.Cratis.Kernel.Engines.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionObserverSubscriber"/>.
/// </summary>
public class ProjectionObserverSubscriber : Grain, IProjectionObserverSubscriber, IProjectionDefinitionObserver
{
    readonly ProviderFor<IProjectionDefinitions> _projectionDefinitionsProvider;
    readonly ProviderFor<IProjectionPipelineDefinitions> _projectionPipelineDefinitionsProvider;
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    ProjectionId _projectionId;
    ProjectionDefinition? _definition;
    ProjectionPipelineDefinition? _pipelineDefinition;
    EngineProjection? _projection;
    IProjectionPipeline? _pipeline;


    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionObserverSubscriber"/> class.
    /// </summary>
    /// <param name="projectionDefinitionsProvider"><see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionPipelineDefinitionsProvider"><see cref="IProjectionPipelineDefinitions"/> for working with pipelines.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> for creating the pipeline for the projection.</param>
    public ProjectionObserverSubscriber(
        ProviderFor<IProjectionDefinitions> projectionDefinitionsProvider,
        ProviderFor<IProjectionPipelineDefinitions> projectionPipelineDefinitionsProvider,
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory)
    {
        _projectionDefinitionsProvider = projectionDefinitionsProvider;
        _projectionPipelineDefinitionsProvider = projectionPipelineDefinitionsProvider;
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
        _projectionId = ProjectionId.NotSet;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        var projection = GrainFactory.GetGrain<IProjection>(_projectionId, new ProjectionKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));
        await projection.SubscribeToDefinitionChanges(this);
    }

    /// <inheritdoc/>
    public void OnDefinitionsChanged()
    {
        Task.Run(HandleDefinitionsAndInstances);
    }

    /// <inheritdoc/>
    public Task OnNext(AppendedEvent @event) => _pipeline?.Handle(@event) ?? Task.CompletedTask;

    async Task HandleDefinitionsAndInstances()
    {
        _definition = await _projectionDefinitionsProvider().GetFor(_projectionId);
        _pipelineDefinition = await _projectionPipelineDefinitionsProvider().GetFor(_projectionId);
        _projection = await _projectionFactory.CreateFrom(_definition);
        _pipeline = _projectionPipelineFactory.CreateFrom(_projection, _pipelineDefinition);
    }
}

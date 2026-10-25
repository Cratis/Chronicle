// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Generations;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

/// <summary>
/// A manager holding one current definition whose later generation is cataloged with its own observer.
/// </summary>
public class a_manager_with_a_cataloged_generation : a_projections_manager_grain
{
    protected readonly IGenerationProjectionResolver _generations = Substitute.For<IGenerationProjectionResolver>();
    protected ProjectionDefinition _definition;
    protected GenerationExecution _execution;
    protected Observation.IObserver _generationObserver;

    protected override IGenerationProjectionResolver GenerationResolver => _generations;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _definition = CreateDefinition(GenerationExecutions.Projection, "the-read-model");
        _state.Projections = [_definition];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        var projection = Substitute.For<Engine.IProjection>();
        projection.EventTypes.Returns([]);
        projection.IsEventSourceKeyed.Returns(true);
        _execution = GenerationExecutions.Create(projection);
        _generations.ResolveCataloged(EventStore, EventStoreNamespaceName.Default, _definition.Identifier).Returns([_execution]);

        _generationObserver = Substitute.For<Observation.IObserver>();
        _observersByKey[new ObserverKey(_execution.Binding.Identity.ObserverId, EventStore, EventStoreNamespaceName.Default, _execution.Binding.EventSequence).ToString()] = _generationObserver;
    }
}

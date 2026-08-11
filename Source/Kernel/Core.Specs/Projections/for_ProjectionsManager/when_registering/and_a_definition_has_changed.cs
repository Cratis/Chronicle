// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_definition_has_changed : given.a_projections_manager_grain
{
    ProjectionDefinition _existing;
    ProjectionDefinition _incoming;

    void Establish()
    {
        _existing = CreateDefinition("the-projection", "the-read-model");
        _incoming = _existing with { IsRewindable = false };
        _state.Projections = [_existing];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact]
    void should_register_the_changed_definition_with_the_engine() =>
        _projectionsServiceClient.Received(1).Register(
            (EventStoreName)EventStore,
            Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _incoming })));

    [Fact] void should_set_the_definition_on_the_projection_grain() => _projectionGrain.Received(1).SetDefinition(_incoming);

    [Fact]
    void should_subscribe_the_observer() =>
        _observerGrain.Received(1).Subscribe<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>());

    [Fact] void should_replace_the_registered_definition() => _state.Projections.ShouldContainOnly(_incoming);
}

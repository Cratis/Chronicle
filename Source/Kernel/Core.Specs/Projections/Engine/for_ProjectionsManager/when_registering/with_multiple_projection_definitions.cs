// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class with_multiple_projection_definitions : given.a_projections_manager
{
    IProjection _firstProjection;
    IProjection _secondProjection;

    async Task Because()
    {
        await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [_firstReadModelDefinition, _secondReadModelDefinition],
            [_namespace]);
    }

    [Fact] void should_be_able_to_retrieve_first_projection() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _firstProjection).ShouldBeTrue();
    [Fact] void should_be_able_to_retrieve_second_projection() => _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _secondProjection).ShouldBeTrue();
    [Fact] void should_create_projections_for_both_definitions() => _projectionFactory.Received(2).Create(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Concepts.Projections.Definitions.ProjectionDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
}

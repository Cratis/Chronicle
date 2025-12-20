// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_then_registering_again_with_updated_definitions : given.a_projections_manager
{
    IProjection _firstProjectionFirstTime;
    IProjection _firstProjectionSecondTime;
    IProjection _secondProjectionFirstTime;
    IProjection _secondProjectionSecondTime;

    async Task Because()
    {
        await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [_firstReadModelDefinition, _secondReadModelDefinition],
            [_namespace]);

        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _firstProjectionFirstTime);
        _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _secondProjectionFirstTime);

        await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [_firstReadModelDefinition, _secondReadModelDefinition],
            [_namespace]);

        _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _firstProjectionSecondTime);
        _manager.TryGet(_eventStore, _namespace, _secondDefinition.Identifier, out _secondProjectionSecondTime);
    }

    [Fact] void should_replace_first_projection_instance() => _firstProjectionSecondTime.ShouldNotEqual(_firstProjectionFirstTime);
    [Fact] void should_replace_second_projection_instance() => _secondProjectionSecondTime.ShouldNotEqual(_secondProjectionFirstTime);
    [Fact] void should_create_projections_twice_for_each_definition() => _projectionFactory.Received(4).Create(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<Concepts.Projections.Definitions.ProjectionDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>());
}

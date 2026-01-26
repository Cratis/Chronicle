// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_adding_namespace;

public class with_existing_definitions : given.a_projections_manager
{
    EventStoreNamespaceName _secondNamespace;
    IProjection _firstProjectionInSecondNamespace;
    IProjection _secondProjectionInSecondNamespace;

    void Establish()
    {
        _secondNamespace = "second-namespace";
    }

    async Task Because()
    {
        await _manager.Register(
            _eventStore,
            [_firstDefinition, _secondDefinition],
            [_firstReadModelDefinition, _secondReadModelDefinition],
            [_namespace]);

        await _manager.AddNamespace(
            _eventStore,
            _secondNamespace,
            [_firstReadModelDefinition, _secondReadModelDefinition]);
    }

    [Fact] void should_create_first_projection_in_second_namespace() => _manager.TryGet(_eventStore, _secondNamespace, _firstDefinition.Identifier, out _firstProjectionInSecondNamespace).ShouldBeTrue();
    [Fact] void should_create_second_projection_in_second_namespace() => _manager.TryGet(_eventStore, _secondNamespace, _secondDefinition.Identifier, out _secondProjectionInSecondNamespace).ShouldBeTrue();
    [Fact] void should_create_projections_for_all_definitions_in_new_namespace() => _projectionFactory.Received(2).Create(_eventStore, _secondNamespace, Arg.Any<Concepts.Projections.Definitions.ProjectionDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_adding_namespace;

public class with_definitions_from_different_event_stores : given.a_projections_manager
{
    EventStoreName _secondEventStore;
    EventStoreNamespaceName _secondNamespace;

    void Establish()
    {
        _secondEventStore = "second-event-store";
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
            _secondEventStore,
            _secondNamespace,
            [_firstReadModelDefinition, _secondReadModelDefinition]);
    }

    [Fact] void should_not_create_projections_for_second_event_store() => _projectionFactory.DidNotReceive().Create(_secondEventStore, Arg.Any<EventStoreNamespaceName>(), Arg.Any<Concepts.Projections.Definitions.ProjectionDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
    [Fact] void should_only_create_projections_for_first_event_store() => _projectionFactory.Received(2).Create(_eventStore, _namespace, Arg.Any<Concepts.Projections.Definitions.ProjectionDefinition>(), Arg.Any<Concepts.ReadModels.ReadModelDefinition>(), Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
}

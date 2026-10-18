// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_another_silo_changes_the_definition : Engine.for_ProjectionsManager.given.a_projections_manager
{
    ProjectionDefinitionStorageProvider _provider;
    GrainState<ProjectionDefinition> _state;
    GrainId _id;

    async Task Establish()
    {
        var stored = _firstDefinition;
        var shared = Substitute.For<IProjectionDefinitionsStorage>();
        shared.Has(_firstDefinition.Identifier).Returns(true);
        shared.Get(_firstDefinition.Identifier).Returns(_ => stored);
        _storage.GetEventStore(_eventStore).Projections.Returns(new CachingProjectionDefinitionsStorage(shared));
        _provider = new(_storage);
        _id = GrainId.Create("projection", new ProjectionKey(_firstDefinition.Identifier, _eventStore).ToString());
        _state = new() { State = _firstDefinition };
        await _provider.ReadStateAsync("definition", _id, _state);
        stored = _firstDefinition with { SubscribesToAllEvents = true };
    }

    Task Because() => _provider.ReadStateAsync("definition", _id, _state);

    [Fact] void should_load_the_other_silos_definition() => _state.State.SubscribesToAllEvents.ShouldBeTrue();
}

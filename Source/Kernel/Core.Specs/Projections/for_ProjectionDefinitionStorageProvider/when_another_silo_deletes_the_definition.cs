// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionDefinitionStorageProvider;

public class when_another_silo_deletes_the_definition : Engine.for_ProjectionsManager.given.a_projections_manager
{
    ProjectionDefinitionStorageProvider _provider;
    GrainState<ProjectionDefinition> _state;
    GrainId _id;

    async Task Establish()
    {
        var exists = true;
        var shared = Substitute.For<IProjectionDefinitionsStorage>();
        shared.Has(_firstDefinition.Identifier).Returns(_ => exists);
        shared.Get(_firstDefinition.Identifier).Returns(_firstDefinition);
        _storage.GetEventStore(_eventStore).Projections.Returns(new CachingProjectionDefinitionsStorage(shared));
        _provider = new(_storage);
        _id = GrainId.Create("projection", new ProjectionKey(_firstDefinition.Identifier, _eventStore).ToString());
        await _provider.ReadStateAsync("definition", _id, new GrainState<ProjectionDefinition>());
        exists = false;
        _state = new();
    }

    Task Because() => _provider.ReadStateAsync("definition", _id, _state);

    [Fact] void should_not_load_a_deleted_definition_on_activation() => _state.State.ShouldBeNull();
}

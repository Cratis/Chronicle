// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_removing_child_from_collection;

public class and_role_was_added : Specification
{
    ReadModelScenario<CollectionWithRemovableRoles> _scenario;
    EventSourceId _collectionId;
    Guid _collectionGuid;
    Guid _roleGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<CollectionWithRemovableRoles>();
        _collectionGuid = Guid.NewGuid();
        _roleGuid = Guid.NewGuid();
        _collectionId = new EventSourceId(_collectionGuid);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_collectionId)
            .Events(
                new CollectionCreated(_collectionGuid),
                new SystemRoleAdded(_collectionGuid, _roleGuid, "Administrator"),
                new RoleRemovedFromCollection(_collectionGuid, _roleGuid));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_no_roles() => _scenario.Instance!.Roles.Count.ShouldEqual(0);
}

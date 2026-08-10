// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_recreating_a_deferred_child_after_a_deferred_removal : Specification
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

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_collectionId)
            .Events(
                new RoleRemovedFromCollection(_collectionGuid, _roleGuid),
                new SystemRoleAdded(_collectionGuid, _roleGuid, "Administrator"),
                new CollectionCreated(_collectionGuid));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_the_recreated_role() => _scenario.Instance!.Roles.Single().Id.ShouldEqual(_roleGuid);
    [Fact] void should_have_only_the_recreated_role() => _scenario.Instance!.Roles.Count.ShouldEqual(1);
}

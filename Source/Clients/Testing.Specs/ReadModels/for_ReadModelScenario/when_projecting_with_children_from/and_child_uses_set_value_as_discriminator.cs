// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_uses_set_value_as_discriminator : Specification
{
    ReadModelScenario<CollectionRoles> _scenario;
    EventSourceId _collectionId;
    Guid _collectionGuid;
    Guid _uiRoleGuid;
    Guid _systemRoleGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<CollectionRoles>();
        _collectionGuid = Guid.NewGuid();
        _uiRoleGuid = Guid.NewGuid();
        _systemRoleGuid = Guid.NewGuid();
        _collectionId = new EventSourceId(_collectionGuid);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_collectionId)
            .Events(new CollectionCreated(_collectionGuid));

        await _scenario.Given
            .ForEventSource(new EventSourceId(_uiRoleGuid))
            .Events(new UIRoleAdded(_collectionGuid, _uiRoleGuid, "My UI Role"));

        await _scenario.Given
            .ForEventSource(new EventSourceId(_systemRoleGuid))
            .Events(new SystemRoleAdded(_collectionGuid, _systemRoleGuid, "My System Role"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_roles() => _scenario.Instance.Roles.Count.ShouldEqual(2);
    [Fact] void should_have_a_ui_role() => _scenario.Instance.HasUiRole.ShouldBeTrue();
    [Fact] void should_have_a_system_role() => _scenario.Instance.HasSystemRole.ShouldBeTrue();
    [Fact] void should_set_ui_role_type_to_ui_role() => _scenario.Instance.Roles.Any(r => r.RoleType == CollectionRoleType.UIRole).ShouldBeTrue();
    [Fact] void should_set_system_role_type_to_system_role() => _scenario.Instance.Roles.Any(r => r.RoleType == CollectionRoleType.SystemRole).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_multiple_root_events_setting_different_values;

/// <summary>
/// Verifies that a root-level model-bound projection applies the correct [SetValue] for each event type.
/// Regression test for: "Multi-[SetValue] on a projected property only applies the first attribute".
/// With a SystemRoleAdded event, RoleType must be SystemRole (not the UIRole value from the first declared [SetValue]).
/// </summary>
public class and_system_role_added_event_is_projected : Specification
{
    ReadModelScenario<RootRole> _scenario;
    Guid _roleGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<RootRole>();
        _roleGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(new EventSourceId(_roleGuid))
            .Events(new SystemRoleAdded(Guid.NewGuid(), _roleGuid, "My System Role"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_name_from_event() => _scenario.Instance.Name.ShouldEqual("My System Role");
    [Fact] void should_set_role_type_to_system_role() => _scenario.Instance.RoleType.ShouldEqual(CollectionRoleType.SystemRole);
}

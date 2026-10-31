// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// A child-only projection has no root creation event, so its root exists only because a child event created it. The kernel
/// stores such a root with the initialization marker unset and reads it back through the child-only visibility. This spec
/// guards against a future marker write in the testing fold: if the fold ever starts marking these roots uninitialized,
/// <see cref="ReadModelScenario{TReadModel}"/> users must still see the root, both listed and by id.
/// </summary>
public class and_the_projection_is_child_only : Specification
{
    ReadModelScenario<ChildOnlyDepotRoster> _scenario;
    EventSourceId _depotId;
    IEnumerable<ChildOnlyDepotRoster> _instances;
    ChildOnlyDepotRoster _instanceById;

    void Establish()
    {
        _scenario = new ReadModelScenario<ChildOnlyDepotRoster>();
        _depotId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_depotId)
            .Events(new ShiftLogged("Operations", "alice", 7m));

        _instances = _scenario.Instances.Values;
        _instanceById = _scenario.InstanceForEventSourceId(_depotId);
    }

    [Fact] void should_expose_the_root_on_the_scenario() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_list_the_root() => _instances.Count().ShouldEqual(1);
    [Fact] void should_return_the_root_by_id() => _instanceById.ShouldNotBeNull();
    [Fact] void should_return_the_child_created_root() => _instanceById.Shifts.Select(_ => _.Worker).ShouldContainOnly("alice");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// The <c>AddChild(...)</c> spelling of <see cref="and_the_same_event_maps_parent_fields_and_a_child"/>.
/// <c>AddChild</c> desugars into <c>Children(...).From&lt;TEvent&gt;(...)</c>, so the same event type both maps a
/// parent scalar and feeds the child collection.
/// </summary>
public class and_the_same_event_maps_parent_fields_and_adds_a_child : Specification
{
    ReadModelScenario<DepotShiftBoard> _scenario;
    EventSourceId _depotId;

    void Establish()
    {
        _scenario = new ReadModelScenario<DepotShiftBoard>();
        _depotId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_depotId)
            .Events(
                new ShiftLogged("Operations", "alice", 7m),
                new ShiftLogged("Operations", "bob", 5m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_the_parent_scalar() => _scenario.Instance!.Depot.ShouldEqual("Operations");
    [Fact] void should_have_two_shift_entries() => _scenario.Instance!.Shifts.Count().ShouldEqual(2);
    [Fact] void should_map_first_child_key() => _scenario.Instance!.Shifts.First().Worker.ShouldEqual("alice");
    [Fact] void should_map_first_child_value() => _scenario.Instance!.Shifts.First().Hours.ShouldEqual(7m);
    [Fact] void should_map_second_child_key() => _scenario.Instance!.Shifts.Last().Worker.ShouldEqual("bob");
    [Fact] void should_map_second_child_value() => _scenario.Instance!.Shifts.Last().Hours.ShouldEqual(5m);
}

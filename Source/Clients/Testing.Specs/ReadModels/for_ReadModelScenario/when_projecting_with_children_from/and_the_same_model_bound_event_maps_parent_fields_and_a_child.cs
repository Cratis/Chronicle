// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// The model-bound spelling of <see cref="and_the_same_event_maps_parent_fields_and_a_child"/>: the same event
/// type is both the root <c>[FromEvent&lt;T&gt;]</c> source and the <c>[ChildrenFrom&lt;T&gt;]</c> source.
/// </summary>
public class and_the_same_model_bound_event_maps_parent_fields_and_a_child : Specification
{
    ReadModelScenario<DepotShiftRegister> _scenario;
    EventSourceId _depotId;

    void Establish()
    {
        _scenario = new ReadModelScenario<DepotShiftRegister>();
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

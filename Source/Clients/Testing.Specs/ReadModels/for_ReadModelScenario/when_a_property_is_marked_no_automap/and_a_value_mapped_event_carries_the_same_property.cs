// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_property_is_marked_no_automap;

/// <summary>
/// Verifies that a property flagged with property-level <c>[NoAutoMap]</c> is not overwritten by AutoMap
/// when another event the read model value-maps happens to carry a property with the same name. This is the
/// case the aggregate heuristic cannot help with (the other event is subscribed for a real value mapping),
/// so it isolates the <c>[NoAutoMap]</c> exclusion in the From AutoMap path.
/// </summary>
public class and_a_value_mapped_event_carries_the_same_property : Specification
{
    ReadModelScenario<ArrangementSummary> _scenario;
    EventSourceId _summaryId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ArrangementSummary>();
        _summaryId = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_summaryId)
            .Events(
                new ArrangementSet("Remote"),
                new WorkModeSet("Hybrid", "Oslo"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_keep_the_explicitly_sourced_location() => _scenario.Instance!.Location.ShouldEqual("Remote");
    [Fact] void should_map_the_value_mapped_property() => _scenario.Instance!.WorkMode.ShouldEqual("Hybrid");
}

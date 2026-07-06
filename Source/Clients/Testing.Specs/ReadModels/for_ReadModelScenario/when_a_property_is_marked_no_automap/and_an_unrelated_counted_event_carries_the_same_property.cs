// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_property_is_marked_no_automap;

/// <summary>
/// Verifies that a property flagged with property-level <c>[NoAutoMap]</c> is not overwritten by AutoMap
/// when an unrelated event the read model subscribes to (here only to <c>[Count]</c> it) happens to carry a
/// property with the same name. The location must keep the value from its explicit
/// <c>[SetFrom&lt;ArrangementSet&gt;]</c> source even after a later <see cref="CandidateSubmitted"/>.
/// </summary>
public class and_an_unrelated_counted_event_carries_the_same_property : Specification
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
                new CandidateSubmitted("Oslo"),
                new CandidateSubmitted("Bergen"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_keep_the_explicitly_sourced_location() => _scenario.Instance!.Location.ShouldEqual("Remote");
    [Fact] void should_count_the_unrelated_events() => _scenario.Instance!.CandidateCount.ShouldEqual(2);
}

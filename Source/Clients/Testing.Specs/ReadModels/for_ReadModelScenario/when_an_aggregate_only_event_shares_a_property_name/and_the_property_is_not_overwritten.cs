// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_an_aggregate_only_event_shares_a_property_name;

/// <summary>
/// Verifies the aggregate heuristic: an event a read model subscribes to only via <c>[Count]</c> (or another
/// aggregate) does not auto-map its other properties, so an identically named property cannot overwrite an
/// explicitly sourced one — without needing a <c>[NoAutoMap]</c> annotation.
/// </summary>
public class and_the_property_is_not_overwritten : Specification
{
    ReadModelScenario<CountedCollisionSummary> _scenario;
    EventSourceId _summaryId;

    void Establish()
    {
        _scenario = new ReadModelScenario<CountedCollisionSummary>();
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
    [Fact] void should_count_the_aggregated_events() => _scenario.Instance!.CandidateCount.ShouldEqual(2);
}

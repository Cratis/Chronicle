// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that the explicit-beats-implicit fix is scoped: when a shared property name has no explicit
/// source, name-based AutoMap still applies from every subscribed event and the last event processed wins.
/// </summary>
public class when_two_subscribed_events_share_an_unsourced_property_name : Specification
{
    ReadModelScenario<AutoMapCollisionUnsourcedSummary> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<AutoMapCollisionUnsourcedSummary>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new WorkArrangementSet("Oslo", 0),
                new CandidateSubmitted("Ola", "Bergen"));

    [Fact] void should_auto_map_the_last_events_location() => _scenario.Instance!.Location.ShouldEqual("Bergen");
    [Fact] void should_still_count_the_candidate() => _scenario.Instance!.CandidateCount.ShouldEqual(1);
}

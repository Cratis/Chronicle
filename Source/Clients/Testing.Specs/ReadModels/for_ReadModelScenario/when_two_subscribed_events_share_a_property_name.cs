// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that when two subscribed events share a property name and the read-model property declares an
/// explicit source, the explicit source is authoritative: name-based AutoMap from the other event does not
/// bleed its same-named value in and overwrite it.
/// </summary>
public class when_two_subscribed_events_share_a_property_name : Specification
{
    ReadModelScenario<AutoMapCollisionSummary> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<AutoMapCollisionSummary>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new WorkArrangementSet("Oslo", 0),
                new CandidateSubmitted("Ola", "Bergen"));

    [Fact] void should_keep_the_explicitly_sourced_location() => _scenario.Instance!.Location.ShouldEqual("Oslo");
    [Fact] void should_still_count_the_candidate() => _scenario.Instance!.CandidateCount.ShouldEqual(1);
}

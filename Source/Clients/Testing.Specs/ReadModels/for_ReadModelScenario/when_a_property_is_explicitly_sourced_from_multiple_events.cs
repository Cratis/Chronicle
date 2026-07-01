// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that a property explicitly sourced from multiple events still aggregates normally — the
/// explicit-beats-implicit merge suppresses only name-based AutoMap, never explicit mappings, so the last
/// explicit source applied wins.
/// </summary>
public class when_a_property_is_explicitly_sourced_from_multiple_events : Specification
{
    ReadModelScenario<MultiSourceLocationSummary> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<MultiSourceLocationSummary>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new WorkArrangementSet("Oslo", 0),
                new CandidateSubmitted("Ola", "Bergen"));

    [Fact] void should_apply_the_last_explicit_source() => _scenario.Instance!.Location.ShouldEqual("Bergen");
}

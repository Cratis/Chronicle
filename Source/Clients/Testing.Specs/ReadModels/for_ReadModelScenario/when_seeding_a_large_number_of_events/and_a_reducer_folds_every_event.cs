// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_large_number_of_events;

public class and_a_reducer_folds_every_event : Specification
{
    const int NumberOfEvents = 2000;

    ReadModelScenario<Tally> _scenario;
    EventSourceId _tallyId;

    void Establish()
    {
        _scenario = new ReadModelScenario<Tally>();
        _tallyId = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_tallyId)
        .Events(Enumerable.Range(0, NumberOfEvents).Select(_ => new Tallied()).ToArray<object>());

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_fold_every_seeded_event() => _scenario.Instance!.Count.ShouldEqual(NumberOfEvents);
}

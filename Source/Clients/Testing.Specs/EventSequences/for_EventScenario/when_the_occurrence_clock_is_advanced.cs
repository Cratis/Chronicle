// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_the_occurrence_clock_is_advanced : Specification
{
    EventScenario _scenario;
    DateTimeOffset[] _occurred;
    readonly DateTimeOffset _first = new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    void Establish()
    {
        _scenario = new();
        _scenario.Time.AdvanceTo(_first);
    }

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("first"));
        _scenario.Time.AdvanceBy(TimeSpan.FromMinutes(5));
        await _scenario.EventLog.Append(EventSourceId.New(), new TestEvent("second"));
        _occurred = (await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First)).Select(@event => @event.Context.Occurred).ToArray();
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_use_scenario_time_for_the_first_event() => _occurred[0].ShouldEqual(_first);
    [Fact] void should_use_advanced_scenario_time_for_the_second_event() => _occurred[1].ShouldEqual(_first.AddMinutes(5));
}

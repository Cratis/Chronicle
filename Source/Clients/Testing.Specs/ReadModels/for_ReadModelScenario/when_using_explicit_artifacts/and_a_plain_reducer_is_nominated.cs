// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_explicit_artifacts;

public class and_a_plain_reducer_is_nominated : Specification
{
    ReadModelScenario<Balance> _first;
    ReadModelScenario<Balance> _second;
    Defaults _firstDefaults;
    Defaults _secondDefaults;

    void Establish()
    {
        _firstDefaults = Create("first-credit", "first-reducer");
        _secondDefaults = Create("second-credit", "second-reducer");
        _first = new(null, _firstDefaults);
        _second = new(null, _secondDefaults);
    }

    async Task Because()
    {
        await _first.Given.ForEventSource(EventSourceId.New()).Events(new Credited(3), new Credited(4));
        await _second.Given.ForEventSource(EventSourceId.New()).Events(new Credited(9));
    }

    [Fact] void should_reduce_the_first_runs_events() => _first.Instance!.Amount.ShouldEqual(7);
    [Fact] void should_reduce_the_second_runs_events_independently() => _second.Instance!.Amount.ShouldEqual(9);
    [Fact] void should_keep_the_first_event_alias() => _firstDefaults.EventTypes.GetEventTypeFor(typeof(Credited)).Id.Value.ShouldEqual("first-credit");
    [Fact] void should_keep_the_second_event_alias() => _secondDefaults.EventTypes.GetEventTypeFor(typeof(Credited)).Id.Value.ShouldEqual("second-credit");

    static Defaults Create(string eventId, string reducerId)
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Credited>(_ => _.WithId(eventId)).RegisterReducer<PlainReducer>(reducerId);
        return new(new InMemoryClientArtifactsProvider(), null, options);
    }

    public record Credited(int Amount);
    public record Balance(int Amount);
    public class PlainReducer
    {
        public Balance Credit(Credited @event, Balance? current) => new((current?.Amount ?? 0) + @event.Amount);
    }
}

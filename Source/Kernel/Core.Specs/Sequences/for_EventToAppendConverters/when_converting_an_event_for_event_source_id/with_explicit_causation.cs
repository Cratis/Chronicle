// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventToAppendConverters.when_converting_an_event_for_event_source_id;

public class with_explicit_causation : Specification
{
    EventForEventSourceId _result;

    void Because() => _result = new Contracts.Sequences.EventForEventSourceId
    {
        EventSourceId = "source",
        EventType = new() { Id = "event", Generation = 1 },
        Content = "{}",
        Causation = [
            new()
            {
                Occurred = DateTimeOffset.UnixEpoch,
                Type = "first",
                Properties = new Dictionary<string, string> { ["marker"] = "first" }
            },
            new()
            {
                Occurred = DateTimeOffset.UnixEpoch,
                Type = "second",
                Properties = new Dictionary<string, string> { ["marker"] = "second" }
            }
        ]
    }.ToApi();

    [Fact] void should_preserve_each_cause_in_order() => _result.Causation!.Select(_ => _.Type).ShouldEqual(["first", "second"]);
    [Fact] void should_preserve_the_causes_properties() => _result.Causation!.Select(_ => _.Properties["marker"]).ShouldEqual(["first", "second"]);
}

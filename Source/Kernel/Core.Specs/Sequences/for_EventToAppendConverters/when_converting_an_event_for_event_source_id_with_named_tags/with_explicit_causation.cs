// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventToAppendConverters.when_converting_an_event_for_event_source_id_with_named_tags;

public class with_explicit_causation : Specification
{
    EventForEventSourceIdWithNamedTags _result;

    void Because() => _result = new Contracts.Sequences.EventForEventSourceIdWithNamedTags
    {
        EventSourceId = "source",
        EventType = new() { Id = "event", Generation = 1 },
        Content = "{}",
        NamedTags = [new() { Name = "account", Value = "one" }],
        Causation = [
            new()
            {
                Occurred = DateTimeOffset.UnixEpoch,
                Type = "per-event",
                Properties = new Dictionary<string, string> { ["marker"] = "specific" }
            }
        ]
    }.ToApi();

    [Fact] void should_preserve_the_per_event_cause() => _result.Causation!.Single().Type.ShouldEqual("per-event");
    [Fact] void should_preserve_its_properties() => _result.Causation!.Single().Properties["marker"].ShouldEqual("specific");
}

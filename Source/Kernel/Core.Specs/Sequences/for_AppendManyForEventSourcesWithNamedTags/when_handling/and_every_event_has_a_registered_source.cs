// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_handling;

public class and_every_event_has_a_registered_source : Sequences.given.an_append_endpoint
{
    AppendManyForEventSourcesWithNamedTags _command;

    void Establish() => _command = new(
        "store",
        "namespace",
        "event-log",
        [
            new("source-one", "Account", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "one")], EventSource: "Account"),
            new("source-two", "Loan", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "two")], EventSource: "Loan")
        ]);

    async Task Because() => await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_preserve_first_events_registered_source() => _appendedEvents[0].EventSource.ShouldEqual(new EventSourceName("Account"));
    [Fact] void should_preserve_second_events_registered_source() => _appendedEvents[1].EventSource.ShouldEqual(new EventSourceName("Loan"));
    [Fact] void should_preserve_first_events_named_tag() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_preserve_second_events_named_tag() => _appendedEvents[1].NamedTags.Single().Value.ShouldEqual("two");
}

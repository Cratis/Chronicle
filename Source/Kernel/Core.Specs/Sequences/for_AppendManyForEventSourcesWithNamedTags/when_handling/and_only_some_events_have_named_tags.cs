// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.when_handling;

public class and_only_some_events_have_named_tags : Sequences.given.an_append_endpoint
{
    AppendManyForEventSourcesWithNamedTags _command;

    void Establish() => _command = new(
        "store",
        "namespace",
        "event-log",
        [
            new("source-one", "Account", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "one")], EventSource: "Account"),
            new("source-two", "Loan", "", "", new EventType("event", 1, false), "{}", null, [], EventSource: "Loan"),
            new("source-three", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "three")]),
            new("source-four", "", "", "", new EventType("event", 1, false), "{}", null, [])
        ]);

    async Task Because() => await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_append_all_events() => _appendedEvents.Length.ShouldEqual(4);
    [Fact] void should_preserve_tagged_events_registered_source() => _appendedEvents[0].EventSource.ShouldEqual(new EventSourceName("Account"));
    [Fact] void should_preserve_untagged_events_registered_source() => _appendedEvents[1].EventSource.ShouldEqual(new EventSourceName("Loan"));
    [Fact] void should_leave_tagged_events_unregistered_source_null() => _appendedEvents[2].EventSource.ShouldBeNull();
    [Fact] void should_leave_untagged_events_unregistered_source_null() => _appendedEvents[3].EventSource.ShouldBeNull();
    [Fact] void should_preserve_first_events_named_tag() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_leave_second_events_named_tags_empty() => _appendedEvents[1].NamedTags.ShouldBeEmpty();
    [Fact] void should_preserve_third_events_named_tag() => _appendedEvents[2].NamedTags.Single().Value.ShouldEqual("three");
    [Fact] void should_leave_fourth_events_named_tags_empty() => _appendedEvents[3].NamedTags.ShouldBeEmpty();
}

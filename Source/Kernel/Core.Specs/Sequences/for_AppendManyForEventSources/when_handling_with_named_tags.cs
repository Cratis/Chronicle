// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources;

public class when_handling_with_named_tags : Sequences.given.an_append_endpoint
{
    async Task Because() => await new AppendManyForEventSourcesWithNamedTags(
        "store",
        "namespace",
        "event-log",
        [new EventForEventSourceIdWithNamedTags("source-one", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "one")]),
         new EventForEventSourceIdWithNamedTags("source-two", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "two")])])
        .Handle(_grainFactory, _causation, _principal);

    [Fact] void should_keep_first_events_named_tag() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_keep_second_events_named_tag() => _appendedEvents[1].NamedTags.Single().Value.ShouldEqual("two");
}

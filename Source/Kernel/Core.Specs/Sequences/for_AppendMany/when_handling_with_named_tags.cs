// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendMany;

public class when_handling_with_named_tags : Sequences.given.an_append_endpoint
{
    async Task Because() => await new AppendManyWithNamedTags(
        "store",
        "namespace",
        "event-log",
        "source",
        [new EventToAppendWithNamedTags(new EventType("event", 1, false), "{}", [new NamedTag("account", "one")]),
         new EventToAppendWithNamedTags(new EventType("event", 1, false), "{}", [new NamedTag("account", "two")])])
        .Handle(_grainFactory, _causation, _principal);

    [Fact] void should_keep_first_events_named_tag() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_keep_second_events_named_tag() => _appendedEvents[1].NamedTags.Single().Value.ShouldEqual("two");
}

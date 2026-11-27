// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace;

public class when_the_named_tag_route_has_a_token : given.a_protected_append_endpoint
{
    async Task Because() => await new AppendWithNamedTags(
        "store",
        "Tenant",
        "event-log",
        "source",
        "",
        "",
        "",
        new EventType("event", 1, false),
        "{}",
        [new NamedTag("account", "one")],
        ExpectedIncarnation: _captured).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_keep_the_named_tags() => _event.NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_only_use_the_protected_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendToNamespace");
}

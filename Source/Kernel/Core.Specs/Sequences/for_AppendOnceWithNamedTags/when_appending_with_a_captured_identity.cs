// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_AppendOnceWithNamedTags;

public class when_appending_with_a_captured_identity : given.a_namespace_bound_keyed_endpoint
{
    Task Because() => _single.ToAppend("tenant").Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_the_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_only_use_the_protected_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendWithIdempotencyKeyToNamespace");
    [Fact] void should_preserve_named_tags() => _appendedEvents.Single().NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_not_refresh_the_identity() => _incarnations.ReceivedCalls().ShouldBeEmpty();
}

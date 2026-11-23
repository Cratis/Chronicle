// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyToNamespace;

public class when_the_namespace_has_been_recreated : given.a_protected_batch_endpoint
{
    Task Because() => Append();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_resolve_the_canonical_name() => _grainFactory.Received().GetGrain<IEventSequence>(new EventSequenceKey("event-log", "store", "Tenant"));
    [Fact] void should_only_use_the_protected_batch_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendManyToNamespace");
    [Fact] void should_not_ensure_the_namespace() => _incarnations.DidNotReceiveWithAnyArgs().Ensure(default!, default!);
    [Fact] void should_preserve_the_source_id() => _appendedEvents.Single().EventSourceId.Value.ShouldEqual("source");
    [Fact] void should_preserve_named_tags() => _appendedEvents.Single().NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_preserve_legacy_tags() => _appendedEvents.Single().Tags.Single().Value.ShouldEqual("batch-tag");
    [Fact] void should_preserve_the_subject() => _appendedEvents.Single().Subject!.Value.ShouldEqual("subject");
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}

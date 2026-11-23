// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendOnceToNamespace;

public class when_the_namespace_has_been_recreated : given.a_namespace_bound_keyed_endpoint
{
    Task Because() => AppendSingle();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_resolve_the_canonical_name() => _grainFactory.Received().GetGrain<IEventSequence>(new EventSequenceKey("event-log", "store", "Tenant"));
    [Fact] void should_only_use_the_protected_keyed_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendWithIdempotencyKeyToNamespace");
    [Fact] void should_not_ensure_the_namespace() => _incarnations.DidNotReceiveWithAnyArgs().Ensure(default!, default!);
    [Fact] void should_preserve_the_key() => _keys.Single().ShouldEqual("key");
    [Fact] void should_preserve_named_tags() => _appendedEvents.Single().NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_preserve_legacy_tags() => _appendedEvents.Single().Tags.Single().Value.ShouldEqual("tag");
    [Fact] void should_preserve_routing() => _appendedEvents.Single().eventStreamId.Value.ShouldEqual("stream");
    [Fact] void should_preserve_the_source_definition() => _appendedEvents.Single().EventSource!.Value.ShouldEqual("definition");
    [Fact] void should_preserve_causation() => _forwardedCausation.Single().Type.Value.ShouldEqual("cause");
    [Fact] void should_preserve_occurrence_time() => _appendedEvents.Single().Occurred.ShouldEqual(_occurred);
    [Fact] void should_preserve_the_settlement() => _singleResult.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_preserve_the_sequence_number() => _singleResult.SequenceNumber.Value.ShouldEqual(7UL);
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}

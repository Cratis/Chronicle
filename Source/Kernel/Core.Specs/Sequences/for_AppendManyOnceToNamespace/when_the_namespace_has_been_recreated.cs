// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnceToNamespace;

public class when_the_namespace_has_been_recreated : given.a_namespace_bound_keyed_endpoint
{
    Task Because() => AppendBatch();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_resolve_the_canonical_name() => _grainFactory.Received().GetGrain<IEventSequence>(new EventSequenceKey("event-log", "store", "Tenant"));
    [Fact] void should_only_use_the_protected_keyed_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendManyWithIdempotencyKeysToNamespace");
    [Fact] void should_not_ensure_the_namespace() => _incarnations.DidNotReceiveWithAnyArgs().Ensure(default!, default!);
    [Fact] void should_preserve_key_order() => _keys.ShouldEqual(new string?[] { "key", null });
    [Fact] void should_preserve_named_tags() => _appendedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_merge_legacy_tags() => _appendedEvents[0].Tags.Select(tag => tag.Value).ShouldEqual(new[] { "tag", "batch-tag" });
    [Fact] void should_preserve_routing() => _appendedEvents[0].eventStreamId.Value.ShouldEqual("stream");
    [Fact] void should_preserve_the_source_definition() => _appendedEvents[0].EventSource!.Value.ShouldEqual("definition");
    [Fact] void should_preserve_per_event_causation() => _appendedEvents[0].Causation!.Single().Type.Value.ShouldEqual("cause");
    [Fact] void should_preserve_occurrence_time() => _appendedEvents[0].Occurred.ShouldEqual(_occurred);
    [Fact] void should_preserve_source_order() => _appendedEvents.Select(@event => @event.EventSourceId.Value).ShouldEqual(new[] { "source", "other" });
    [Fact] void should_preserve_settlement_order() => _batchResult.Outcomes.ShouldEqual(new[] { AppendOutcome.AlreadyAppended, AppendOutcome.Committed });
    [Fact] void should_preserve_sequence_number_order() => _batchResult.SequenceNumbers.Select(number => number.Value).ShouldEqual(new ulong[] { 7, 8 });
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}

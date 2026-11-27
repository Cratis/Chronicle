// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesToNamespace;

public class when_the_namespace_has_been_recreated : given.a_protected_batch_endpoint
{
    Task Because() => Append();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_resolve_the_canonical_name() => _grainFactory.Received().GetGrain<IEventSequence>(new EventSequenceKey("event-log", "store", "Tenant"));
    [Fact] void should_only_use_the_protected_conjunction_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendManyWithConjunctionsToNamespace");
    [Fact] void should_not_ensure_the_namespace() => _incarnations.DidNotReceiveWithAnyArgs().Ensure(default!, default!);
    [Fact] void should_preserve_repeated_source_conjunctions() => _conjunctions.Entries.Count.ShouldEqual(2);
    [Fact] void should_preserve_named_tags() => _appendedEvents.Single().NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_merge_legacy_tags() => _appendedEvents.Single().Tags.Select(tag => tag.Value).ShouldEqual(new[] { "event-tag", "batch-tag" });
    [Fact] void should_preserve_routing() => _appendedEvents.Single().eventStreamId.Value.ShouldEqual("stream");
    [Fact] void should_preserve_the_source_definition() => _appendedEvents.Single().EventSource!.Value.ShouldEqual("definition");
    [Fact] void should_preserve_per_event_causation() => _appendedEvents.Single().Causation!.Single().Type.Value.ShouldEqual("event-cause");
    [Fact] void should_preserve_occurrence_time() => _appendedEvents.Single().Occurred.ShouldEqual(_occurred);
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}

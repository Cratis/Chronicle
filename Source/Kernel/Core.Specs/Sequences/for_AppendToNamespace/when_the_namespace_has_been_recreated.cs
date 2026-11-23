// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendToNamespace;

public class when_the_namespace_has_been_recreated : given.a_protected_append_endpoint
{
    Task Because() => Append();

    [Fact] void should_keep_the_captured_identity() => _forwarded.ShouldEqual(_captured);
    [Fact] void should_resolve_the_canonical_name() => _grainFactory.Received().GetGrain<IEventSequence>(new EventSequenceKey("event-log", "store", "Tenant"));
    [Fact] void should_only_use_the_protected_grain_method() => _eventSequence.ReceivedCalls().Single().GetMethodInfo().Name.ShouldEqual("AppendToNamespace");
    [Fact] void should_not_ensure_the_namespace() => _incarnations.DidNotReceiveWithAnyArgs().Ensure(default!, default!);
    [Fact] void should_preserve_the_source_id() => _event.EventSourceId.Value.ShouldEqual("source");
    [Fact] void should_dispatch_successfully() => _error.ShouldBeNull();
}

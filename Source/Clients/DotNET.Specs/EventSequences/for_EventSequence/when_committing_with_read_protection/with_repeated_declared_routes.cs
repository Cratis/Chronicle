// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSources;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_repeated_declared_routes : given.a_trusted_commit
{
    int _enumerations;
    int _tailCalls;

    void Establish() => _sequences.When(_ => _.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>())).Do(_ => ++_tailCalls);

    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(EventSequenceId.Log, Events(), []);
        await Commit();
    });

    [Fact] void should_not_rerun_the_event_provider() => _enumerations.ShouldEqual(1);
    [Fact] void should_resolve_the_shared_routing_predicate_once() => _tailCalls.ShouldEqual(1);
    [Fact] void should_append_both_events() => _request.Events.Count().ShouldEqual(2);
    [Fact] void should_complete_successfully() => _error.ShouldBeNull();

    IEnumerable<EventForEventSourceId> Events()
    {
        ++_enumerations;
        yield return new("source", "first") { EventStreamType = "numbering", DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType };
        yield return new("source", "second") { EventStreamType = "numbering", DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType };
    }
}

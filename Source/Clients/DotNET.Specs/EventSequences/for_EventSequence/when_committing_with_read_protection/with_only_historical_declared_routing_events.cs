// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_only_historical_declared_routing_events : given.a_trusted_commit
{
    void Establish() => _sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>())
        .Returns(Contracts.Queries.QueryResult<EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 0 }));

    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(
            EventSequenceId.Log,
            [new("source", "planned") { EventStreamType = "numbering", DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType }],
            []);
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_keep_the_global_read_boundary_on_the_read_guard() => _unit.GetAppendConditions()[0].After.ShouldEqual((EventSequenceNumber)100u);
    [Fact] void should_keep_the_read_types_on_the_read_guard() => _unit.GetAppendConditions()[0].EventTypes.ShouldContainOnly(_changed);
    [Fact] void should_keep_the_filtered_tail_only_on_the_narrowed_routing_guard() => _unit.GetAppendConditions()[1].After.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_narrow_the_routing_guard_to_its_stream() => _unit.GetAppendConditions()[1].EventStreamType.Value.ShouldEqual("numbering");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_only_historical_declared_routing_events_without_kernel_conjunction_support : given.a_trusted_commit
{
    void Establish()
    {
        ((IKernelCapabilities)_connection).Capabilities.Returns([]);
        _sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>(), Arg.Any<CallContext>())
            .Returns(Contracts.Queries.QueryResult<EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 0 }));
    }

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
    [Fact] void should_send_one_guard_for_the_source() => _request.ConcurrencyScopes.Select(_ => _.EventSourceId).ShouldEqual(["source"]);
    [Fact] void should_keep_the_global_read_boundary_instead_of_the_filtered_tail() => _unit.GetAppendConditions().Single().After.ShouldEqual((EventSequenceNumber)100u);
    [Fact] void should_cover_the_routing_stream() => _unit.GetAppendConditions().Single().EventStreamType.ShouldBeNull();
}

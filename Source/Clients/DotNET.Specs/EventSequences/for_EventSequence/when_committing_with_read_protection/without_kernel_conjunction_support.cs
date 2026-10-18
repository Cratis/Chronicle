// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class without_kernel_conjunction_support : given.a_trusted_commit
{
    void Establish() => ((IKernelCapabilities)_connection).Capabilities.Returns([]);

    Task Because() => Execute(async () =>
    {
        _unit.AddDecisionRead(new DecisionRead<object>("source", null, "store", "tenant", 100, [_changed], new DecisionReadStreamScope("source", "Transactions", "January", null)));
        _unit.AddDecisionRead(new DecisionRead<object>("source", null, "store", "tenant", 80, [_changed], new DecisionReadStreamScope("source", "Transactions", "February", null)));
        _unit.AddEvents(
            EventSequenceId.Log,
            [new("source", "first") { EventStreamType = "first", DeclaredConcurrency = ConcurrencyDimensions.EventStreamType }],
            []);
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_send_two_independent_labels() => _request.ConcurrencyScopes.Select(_ => _.EventSourceId).ShouldEqual(["source", "$read-protection/routing/source/0"]);
    [Fact] void should_keep_the_earliest_read_boundary() => _unit.GetAppendConditions()[0].After.ShouldEqual((EventSequenceNumber)80u);
    [Fact] void should_cover_both_read_streams() => _unit.GetAppendConditions()[0].EventStreamId.ShouldBeNull();
    [Fact] void should_keep_the_routing_predicate() => _unit.GetAppendConditions()[1].EventStreamType.Value.ShouldEqual("first");
}

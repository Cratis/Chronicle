// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_sequential_stream_read_after_staging : given.a_trusted_commit
{
    Task Because() => Execute(() =>
    {
        var read = new ContributedRead(ConcurrencyScopeOrigin.Explicit, "source", new ConcurrencyScope(100, "source", "reservation", "january", EventTypes: [_changed]), "store", "tenant", EventSequenceId.Log, "outer stream decision");
        _unit.EnrolContributedRead(read);
        _unit.AddEvents(EventSequenceId.Log, [new("source", "reserved") { EventStreamType = "reservation", EventStreamId = "january" }], [new("source", read.Scope)]);
        using var nested = _unit.EnforceReadProtection();
        _unit.EnrolContributedRead(read with { Description = "nested stream decision" });
        return Task.CompletedTask;
    });

    [Fact] void should_refuse_the_second_read_before_exposing_the_same_committed_state() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
    [Fact] void should_leave_only_the_outer_reservation_staged() => _unit.GetEvents().ShouldContainOnly("reserved");
    [Fact] void should_not_append() => _request.ShouldBeNull();
}

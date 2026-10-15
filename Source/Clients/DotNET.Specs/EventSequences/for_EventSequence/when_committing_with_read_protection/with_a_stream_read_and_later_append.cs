// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_stream_read_and_later_append : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        var scope = new ConcurrencyScope(40, "source", "reservations", "january", EventTypes: [_changed]);
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Explicit, "source", scope, "store", "tenant", EventSequenceId.Log, "stream read"));
        _unit.AddEvents(EventSequenceId.Log, [new("source", "reserved") { EventStreamType = "reservations", EventStreamId = "january" }], [new("source", scope with { EventTypes = [_changed] })]);
        await Commit();
    });

    [Fact] void should_accept_the_equivalent_append_scope() => _error.ShouldBeNull();
    [Fact] void should_send_one_condition_for_the_stream() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
    [Fact] void should_keep_the_stream_predicate() => _unit.GetAppendConditions().Single().EventStreamId.Value.ShouldEqual("january");
}

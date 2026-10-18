// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_distinct_aggregate_handled_types : given.a_trusted_commit
{
    Task Because() => Execute(() =>
    {
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new(40, "source", EventTypes: [_changed]), "store", "tenant", EventSequenceId.Log, "balance aggregate"));
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new(40, "source", EventTypes: [new EventType("name-changed", 1)]), "store", "tenant", EventSequenceId.Log, "name aggregate"));
        return Task.CompletedTask;
    });

    [Fact] void should_accept_both_aggregate_predicates() => _error.ShouldBeNull();
    [Fact] void should_not_append() => _request.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_rehydration_and_later_aggregate_apply : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        var scope = new ConcurrencyScope(40, "source", EventTypes: [_changed]);
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", scope, "store", "tenant", EventSequenceId.Log, "rehydrated aggregate"));
        _unit.AddEventWithOptions(EventSequenceId.Log, "source", "applied", Causation.Unknown(), new()
        {
            Origin = ConcurrencyScopeOrigin.Aggregate,
            ConcurrencyScope = scope with { EventTypes = [_changed] }
        });
        await Commit();
    });

    [Fact] void should_accept_the_equivalent_apply_scope() => _error.ShouldBeNull();
    [Fact] void should_send_one_condition_for_the_aggregate() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
    [Fact] void should_keep_the_rehydration_boundary() => _unit.GetAppendConditions().Single().After.Value.ShouldEqual(40UL);
    [Fact] void should_keep_the_aggregate_origin() => _unit.GetAppendConditions().Single().Origins.ShouldContainOnly(AppendConditionOrigin.Aggregate);
}

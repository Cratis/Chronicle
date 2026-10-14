// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_an_aggregate_self_commit : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Guard(3, "read-key");
        Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Aggregate);
    }
    async Task Because()
    {
        await _unit.Commit();
        Guard(1, "late-key");
    }
    [Fact] void should_apply_the_aggregate_guard_without_ownership() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
    [Fact] void should_apply_other_observed_labels() => Assert.Equivalent(new ConcurrencyScope(3, "read-key").ToSequencesContract(), ScopeFor("read-key"));
    [Fact] void should_not_append_the_later_read_guard() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
}

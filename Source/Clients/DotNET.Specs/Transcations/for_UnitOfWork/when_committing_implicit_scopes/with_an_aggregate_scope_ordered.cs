// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_an_aggregate_scope_ordered : given.a_unit_with_implicit_scopes
{
    protected override bool Ordered => true;
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId, EventStreamType: "items"), ConcurrencyScopeOrigin.Aggregate);
    }
    Task Because() => Commit();
    [Fact] void should_commit_without_ownership() => _exception.ShouldBeNull();
    [Fact] void should_not_use_the_old_filtered_tail() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}

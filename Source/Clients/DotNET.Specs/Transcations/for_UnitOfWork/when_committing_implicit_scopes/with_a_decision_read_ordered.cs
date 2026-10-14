// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_decision_read_ordered : given.a_unit_with_implicit_scopes
{
    protected override bool Ordered => true;
    void Establish()
    {
        Guard();
        Read(3);
        Enroll();
    }
    Task Because() => Commit(true);
    [Fact] void should_commit_as_owner() => _exception.ShouldBeNull();
    [Fact] void should_use_the_earliest_global_boundary_without_types() => Assert.Equivalent(new ConcurrencyScope(3, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}

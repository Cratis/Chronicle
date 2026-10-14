// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_rejected_explicit_enrollment : given.a_unit_with_implicit_scopes
{
    Exception _rejected;
    protected override bool Ordered => true;
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Aggregate);
        _rejected = Catch.Exception(() => Enroll(new(2, _sourceId)));
    }
    Task Because() => Commit();
    [Fact] void should_reject_the_conflicting_scope() => _rejected.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_not_record_origin_for_a_rejected_enrollment() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}

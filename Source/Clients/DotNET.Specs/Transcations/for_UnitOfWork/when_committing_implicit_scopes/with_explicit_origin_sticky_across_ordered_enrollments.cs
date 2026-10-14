// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_explicit_origin_sticky_across_ordered_enrollments : given.a_unit_with_implicit_scopes
{
    protected override bool Ordered => true;
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId));
        Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Aggregate);
    }
    Task Because() => Commit();
    [Fact] void should_preserve_explicit_displacement() => Assert.Equivalent(new ConcurrencyScope(1, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}

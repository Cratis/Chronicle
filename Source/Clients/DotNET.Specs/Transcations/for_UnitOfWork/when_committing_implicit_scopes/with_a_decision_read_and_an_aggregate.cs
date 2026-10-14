// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_decision_read_and_an_aggregate : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Read(3);
    }
    void Because() => _exception = Catch.Exception(() => Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Aggregate));
    [Fact] void should_preserve_the_conflict() => _exception.ShouldBeOfExactType<DecisionReadConflictsWithConcurrencyScope>();
}

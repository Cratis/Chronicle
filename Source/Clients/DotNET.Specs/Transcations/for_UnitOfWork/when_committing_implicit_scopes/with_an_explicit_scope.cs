// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_an_explicit_scope : given.a_unit_with_implicit_scopes
{
    readonly ConcurrencyScope _explicit = new(1, EventStreamType: "items");
    void Establish()
    {
        Guard();
        Enroll(_explicit);
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_explicit_scope_unchanged() => Assert.Equivalent(_explicit.ToSequencesContract(), ScopeFor(_sourceId));
}

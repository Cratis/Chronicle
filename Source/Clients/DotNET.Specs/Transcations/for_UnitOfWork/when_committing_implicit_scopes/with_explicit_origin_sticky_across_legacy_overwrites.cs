// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_explicit_origin_sticky_across_legacy_overwrites : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId));
        Enroll(new(2, _sourceId), ConcurrencyScopeOrigin.Aggregate);
    }
    Task Because() => Commit();
    [Fact] void should_keep_legacy_last_wins_and_explicit_displacement() => Assert.Equivalent(new ConcurrencyScope(2, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}

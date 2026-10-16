// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_an_incomplete_strategy_scope : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(EventSequenceNumber.Unavailable, _label, EventTypes: [_stockReduced]), "strategy-issued empty scope");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_a_scope_without_a_checked_boundary() => _error.ShouldBeOfExactType<Transactions.ExplicitScopeDoesNotCoverReads>();
}

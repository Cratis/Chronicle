// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_declared_routing_not_subsumed_by_explicit : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label, EventTypes: [_stockReduced]), "client version");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(95, _label, "Transactions"), "transactions");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_dropping_declared_routing() => _error.ShouldBeOfExactType<Transactions.DeclaredRoutingNotCoveredByExplicitScope>();
}

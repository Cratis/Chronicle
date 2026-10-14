// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_an_exact_boundary_after_a_declared_filtered_tail : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "client version");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, _label, "Transactions"), "transactions");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_an_exact_boundary_that_cannot_cover_routing() => _error.ShouldBeOfExactType<Transactions.DeclaredRoutingNotCoveredByExplicitScope>();
}

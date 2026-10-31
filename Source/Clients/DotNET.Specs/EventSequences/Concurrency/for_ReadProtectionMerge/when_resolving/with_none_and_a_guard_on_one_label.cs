// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_none_and_a_guard_on_one_label : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.Explicit, ConcurrencyScope.None);
        Add(ReadProtectionContributionKind.Explicit, new(7, _label, "stream"));
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_the_ambiguous_intent() => _error.ShouldBeOfExactType<Transactions.ConflictingContributionsOnLabel>();
}

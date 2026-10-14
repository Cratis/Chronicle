// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_custom_strategy : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.CustomRouting, new(90, _label), "custom strategy");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_require_the_default_strategy() => _error.ShouldBeOfExactType<Transactions.ReadProtectionRequiresTheDefaultConcurrencyStrategy>();
}

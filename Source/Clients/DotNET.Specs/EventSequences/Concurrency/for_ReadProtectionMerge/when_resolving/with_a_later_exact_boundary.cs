// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_later_exact_boundary : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(101, _label), "source tail at 101");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_displacing_the_read_boundary() => _error.ShouldBeOfExactType<ExplicitScopeDoesNotCoverReads>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_distinct_explicit_scopes_on_a_read_label : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "first version");
        Add(ReadProtectionContributionKind.Explicit, new(80, _label), "second version");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_distinct_explicit_scopes_instead_of_selecting_one() => _error.ShouldBeOfExactType<Transactions.ConflictingExplicitScopes>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_an_exact_stream_scope : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label, "Approval", "owner-1:2026-10"), "approval stream");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_narrowing_a_whole_source_read() => _error.ShouldBeOfExactType<Transactions.ExplicitScopeDoesNotCoverReads>();
}

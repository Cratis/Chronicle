// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_negative_read_and_actual_explicit_boundary : given.contributions
{
    void Establish()
    {
        Read(EventSequenceNumber.BeforeFirst);
        Add(ReadProtectionContributionKind.Explicit, new(0, _label), "client version");
    }

    void Because() => _error = Catch.Exception(() => Merge());

    [Fact] void should_refuse_losing_the_creation_guard() => _error.ShouldBeOfExactType<Transactions.ExplicitScopeDoesNotCoverReads>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_reads_with_different_mutation_epochs : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.ProtectedRead, new((EventSequenceNumber)5, _label, EventTypes: [_stockReduced]) { MutationEpoch = 6UL });
        Add(ReadProtectionContributionKind.ProtectedRead, new((EventSequenceNumber)7, _label, EventTypes: [_stockReduced]) { MutationEpoch = 4UL });
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_the_oldest_epoch() => _plan.Scopes.Single().Value.MutationEpoch.ShouldEqual(4UL);
}

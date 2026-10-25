// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_covering_scope_on_a_kernel_without_conjunctions : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.ProtectedRead, new((EventSequenceNumber)5, _label, EventTypes: [_stockReduced]) { MutationEpoch = 6UL });
        Add(ReadProtectionContributionKind.Explicit, new((EventSequenceNumber)5, _label, EventTypes: [_stockReduced]), "explicit guard");
    }

    void Because() => _plan = Merge(supportsConjunctions: false);

    [Fact] void should_carry_the_epoch_onto_the_covering_scope() => _plan.Scopes.Single().Value.MutationEpoch.ShouldEqual(6UL);
}

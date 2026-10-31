// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class without_kernel_conjunction_support : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.ProtectedRead, new(100, _label, "Transactions", "January", EventTypes: [_stockReduced]), "January stock");
        Add(ReadProtectionContributionKind.ProtectedRead, new(80, _label, "Transactions", "February", EventTypes: [_stockRemoved]), "February stock");
    }

    void Because() => _plan = Merge(supportsConjunctions: false);

    [Fact] void should_merge_the_reads_into_one_guard() => _plan.Scopes.Count.ShouldEqual(1);
    [Fact] void should_keep_the_earliest_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)80u);
    [Fact] void should_cover_both_streams() => _plan.Scopes[0].Value.EventStreamId.ShouldBeNull();
    [Fact] void should_cover_both_event_types() => _plan.Scopes[0].Value.EventTypes.ShouldContainOnly(_stockReduced, _stockRemoved);
    [Fact] void should_retain_both_read_descriptions() => _plan.Conditions[0].Reads.ShouldContainOnly("January stock", "February stock");
}

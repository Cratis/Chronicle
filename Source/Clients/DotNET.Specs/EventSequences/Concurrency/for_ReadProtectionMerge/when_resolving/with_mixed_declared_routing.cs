// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_mixed_declared_routing : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, _label, "Transactions"), "transactions");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(60, EventStreamType: "Numbering"), "numbering");
    }

    void Because() => _plan = Merge();

    [Fact] void should_merge_source_narrowed_routing_at_the_read_boundary() => _plan.Scopes[_label].SequenceNumber.ShouldEqual((Events.EventSequenceNumber)100u);
    [Fact] void should_widen_the_read_types() => _plan.Scopes[_label].EventTypes.ShouldBeEmpty();
    [Fact] void should_keep_source_less_routing_separately() => _plan.Scopes.Count.ShouldEqual(2);
}

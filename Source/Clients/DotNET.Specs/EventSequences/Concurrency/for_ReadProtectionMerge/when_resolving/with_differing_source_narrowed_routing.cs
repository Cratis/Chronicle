// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_differing_source_narrowed_routing : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, _label, "Transactions"), "transactions");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(60, _label, "Approval"), "approval");
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_the_read_boundary_instead_of_the_filtered_tails() => _plan.Scopes[_label].SequenceNumber.ShouldEqual((EventSequenceNumber)100u);
    [Fact] void should_merge_instead_of_refusing_differing_routing_predicates() => _plan.Scopes.Count.ShouldEqual(1);
    [Fact] void should_coarsen_the_guard_to_any_type() => _plan.Scopes[_label].EventTypes.ShouldBeEmpty();
}

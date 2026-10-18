// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_declared_filtered_tail_below_the_read_boundary_without_kernel_conjunction_support : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.DeclaredRouting, new(0, _label, "Numbering"), "numbering");
    }

    void Because() => _plan = Merge(supportsConjunctions: false);

    [Fact] void should_merge_the_routing_into_the_read_guard() => _plan.Scopes.Count.ShouldEqual(1);
    [Fact] void should_keep_the_read_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)100u);
    [Fact] void should_cover_the_routing_stream() => _plan.Scopes[0].Value.EventStreamType.ShouldBeNull();
    [Fact] void should_cover_the_routing_event_types() => _plan.Scopes[0].Value.EventTypes!.ShouldBeEmpty();
    [Fact] void should_record_the_routing_origin() => _plan.Conditions[0].Origins.ShouldContain(AppendConditionOrigin.DeclaredRouting);
}

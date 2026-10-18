// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_covering_explicit_and_source_less_routing_on_a_capable_kernel : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "client version");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, EventStreamType: "Numbering"), "numbering");
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_both_predicates_on_the_same_label() => _plan.Scopes.Select(_ => _.Key).ShouldEqual([_label, _label]);
    [Fact] void should_keep_the_exact_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)90u);
    [Fact] void should_keep_the_routing_boundary() => _plan.Scopes[1].Value.SequenceNumber.ShouldEqual((EventSequenceNumber)40u);
    [Fact] void should_keep_routing_source_less() => _plan.Scopes[1].Value.EventSourceId.ShouldBeNull();
}

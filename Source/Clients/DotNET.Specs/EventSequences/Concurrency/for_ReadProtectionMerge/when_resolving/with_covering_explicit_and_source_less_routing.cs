// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_covering_explicit_and_source_less_routing : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "client version");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, EventStreamType: "Numbering"), "numbering");
    }

    void Because() => _plan = Merge(supportsConjunctions: false);

    [Fact] void should_keep_the_exact_condition_boundary() => _plan.Scopes[0].Value.SequenceNumber.ShouldEqual((Events.EventSequenceNumber)90u);
    [Fact] void should_keep_the_routing_boundary() => _plan.Scopes[1].Value.SequenceNumber.ShouldEqual((Events.EventSequenceNumber)40u);
    [Fact] void should_reserve_a_separate_label_for_routing() => _plan.Scopes[1].Key.Value.ShouldEqual("$read-protection/routing/stock/0");
    [Fact] void should_keep_routing_source_less() => _plan.Scopes[1].Value.EventSourceId.ShouldBeNull();
}

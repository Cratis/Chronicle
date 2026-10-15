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

    void Because() => _plan = Merge();

    [Fact] void should_keep_the_exact_condition_boundary() => _plan.Scopes[_label].SequenceNumber.ShouldEqual((Events.EventSequenceNumber)90u);
    [Fact] void should_keep_routing_on_its_own_label() => _plan.Scopes["$read-protection/routing/stock/0"].SequenceNumber.ShouldEqual((Events.EventSequenceNumber)40u);
    [Fact] void should_keep_routing_source_less() => _plan.Scopes["$read-protection/routing/stock/0"].EventSourceId.ShouldBeNull();
}

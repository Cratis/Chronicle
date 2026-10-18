// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_two_source_less_routing_conditions : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.DeclaredRouting, new(40, EventStreamType: "Numbering"), "numbering");
        Add(ReadProtectionContributionKind.DeclaredRouting, new(60, EventStreamType: "Approval"), "approval");
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_both_conditions_separately() => _plan.Scopes.Count.ShouldEqual(3);
    [Fact] void should_keep_the_first_routing_predicate() => _plan.Scopes[1].Value.EventStreamType!.Value.ShouldEqual("Numbering");
    [Fact] void should_keep_the_second_routing_predicate() => _plan.Scopes[2].Value.EventStreamType!.Value.ShouldEqual("Approval");
}

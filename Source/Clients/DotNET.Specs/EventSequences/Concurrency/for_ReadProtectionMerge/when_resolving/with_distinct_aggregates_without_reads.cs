// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_distinct_aggregates_without_reads : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.Aggregate, new(90, _label, EventTypes: [_stockReduced]), "stock aggregate");
        Add(ReadProtectionContributionKind.Aggregate, new(90, _label, EventTypes: [_stockRemoved]), "removal aggregate");
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_every_distinct_contribution() => _plan.Scopes.Count.ShouldEqual(2);
}

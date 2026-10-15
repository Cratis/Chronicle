// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_equal_type_ids_and_different_generations : given.contributions
{
    void Establish()
    {
        Read(100);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label, EventTypes: [_stockReduced]));
        Add(ReadProtectionContributionKind.Explicit, new(90, _label, EventTypes: [new EventType(_stockReduced.Id, 2)]));
    }

    void Because() => _plan = Merge();

    [Fact] void should_compare_type_sets_by_id_instead_of_generation() => _plan.Scopes.Count.ShouldEqual(1);
}

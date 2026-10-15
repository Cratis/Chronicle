// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_a_covering_no_matching_event_scope : given.contributions
{
    void Establish()
    {
        Read(EventSequenceNumber.BeforeFirst);
        Add(ReadProtectionContributionKind.Explicit, new(EventSequenceNumber.BeforeFirst, _label), "creation guard");
    }

    void Because() => _plan = Merge();

    [Fact] void should_keep_the_negative_condition() => _plan.Scopes[_label].ExpectsNoMatchingEvent.ShouldBeTrue();
}

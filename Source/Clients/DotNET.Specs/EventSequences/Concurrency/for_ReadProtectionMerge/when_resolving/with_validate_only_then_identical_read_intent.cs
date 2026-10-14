// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.when_resolving;

public class with_validate_only_then_identical_read_intent : given.contributions
{
    void Establish()
    {
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "validate version", ReadProtectionContributionIntent.ValidateOnly);
        Add(ReadProtectionContributionKind.Explicit, new(90, _label), "stream read", ReadProtectionContributionIntent.ReadEnrolled);
    }

    void Because() => _plan = Merge(false);

    [Fact] void should_preserve_validate_only_intent_in_either_order() => _plan.Scopes.Count.ShouldEqual(1);
}

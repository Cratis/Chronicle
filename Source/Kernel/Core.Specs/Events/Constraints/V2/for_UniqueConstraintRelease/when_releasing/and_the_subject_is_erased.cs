// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_the_subject_is_erased : given.a_real_release
{
    async Task Establish()
    {
        await StoreProtectedName("Alice");
        await Erase();
    }

    async Task Because() => _result = await Create();

    [Fact] void should_make_no_claim() => _result.Outcome.ShouldEqual(UniqueConstraintReleaseOutcome.NoClaimErased);
    [Fact] void should_have_no_components() => _result.Components.Count.ShouldEqual(0);
}

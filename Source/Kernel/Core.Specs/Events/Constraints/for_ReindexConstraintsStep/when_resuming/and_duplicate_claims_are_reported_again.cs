// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_resuming;

public class and_duplicate_claims_are_reported_again : given.a_reindex_with_duplicate_claims
{
    string _firstHash;
    string _secondHash;

    async Task Because()
    {
        await Perform();
        _firstHash = (await _step.GetState()).CollisionSamples[_unique.Name][0].ValueHash;
        await Perform();
        _secondHash = (await _step.GetState()).CollisionSamples[_unique.Name][0].ValueHash;
    }

    [Fact] void should_change_the_hash_when_the_step_runs_again() => _secondHash.ShouldNotEqual(_firstHash);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_the_subscription_is_collapsing : given.a_bucket_replay
{
    protected override bool Collapsing => true;

    async Task Because() => await _job.PrepareStepsForTesting(_request);

    [Fact] void should_keep_the_ordered_path() => _stateStorage.State.ReplayBucketSteps.ShouldBeEmpty();
}

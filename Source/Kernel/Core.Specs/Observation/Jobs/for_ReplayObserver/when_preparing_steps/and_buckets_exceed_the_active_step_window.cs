// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_buckets_exceed_the_active_step_window : given.a_bucket_replay
{
    protected override int BucketCount => 32;

    async Task Because() => await _job.PrepareStepsForTesting(_request);

    [Fact] void should_plan_every_bucket() => _stateStorage.State.ReplayBucketSteps.Count.ShouldEqual(32);
    [Fact] void should_keep_the_existing_active_step_cap() => _job.MaxActiveStepsForTesting.ShouldEqual(16);
}

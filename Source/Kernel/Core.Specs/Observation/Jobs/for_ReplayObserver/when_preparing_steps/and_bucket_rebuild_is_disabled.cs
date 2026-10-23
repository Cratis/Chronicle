// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_bucket_rebuild_is_disabled : given.a_bucket_replay
{
    protected override int BucketCount => 0;

    async Task Because() => await _job.PrepareStepsForTesting(_request);

    [Fact] void should_leave_bucket_progress_empty() => _stateStorage.State.ReplayBucketSteps.ShouldBeEmpty();
    [Fact] void should_not_read_the_projection_definition() => _eventStoreStorage.Projections.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.Concepts.Projections.ProjectionId>());
}

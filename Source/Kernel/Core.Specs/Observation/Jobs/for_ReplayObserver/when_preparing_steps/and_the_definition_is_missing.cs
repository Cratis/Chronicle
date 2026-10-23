// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_the_definition_is_missing : given.a_bucket_replay
{
    void Establish() => _eventStoreStorage.Projections.Has(Arg.Any<ProjectionId>()).Returns(false);

    async Task Because() => await _job.PrepareStepsForTesting(_request);

    [Fact] void should_keep_the_ordered_path() => _stateStorage.State.ReplayBucketSteps.ShouldBeEmpty();
    [Fact] void should_not_try_reading_the_missing_definition() => _eventStoreStorage.Projections.DidNotReceive().Get(Arg.Any<ProjectionId>());
}

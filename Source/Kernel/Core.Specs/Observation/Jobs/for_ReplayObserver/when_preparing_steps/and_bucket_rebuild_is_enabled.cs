// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_bucket_rebuild_is_enabled : given.a_bucket_replay
{
    IImmutableList<JobStepDetails> _steps;

    async Task Because() => _steps = await _job.PrepareStepsForTesting(_request);

    [Fact] void should_plan_exactly_eight_steps() => _steps.Count.ShouldEqual(8);
    [Fact] void should_pin_every_bucket_in_the_arguments() => _steps.Select(step => ((HandleEventsForObserverArguments)step.Request).Bucket).ShouldEqual(Enumerable.Range(0, 8).Select(index => new ReplayBucket(index, 8, 1)));
    [Fact] void should_persist_only_one_progress_entry_per_bucket() => _stateStorage.State.ReplayBucketSteps.Count.ShouldEqual(8);
    [Fact] void should_limit_active_steps_to_the_bucket_count() => _job.MaxActiveStepsForTesting.ShouldEqual(8);
    [Fact] void should_not_enumerate_partitions() => _keyIndexes.DidNotReceive().GetFor(Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverKey>(), Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverReaderScope>());
}

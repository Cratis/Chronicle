// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_a_partition_hold_has_no_owner : given.a_catch_up_observer_job
{
    void Establish()
    {
        _observer.GetState().Returns(new ObserverState { CatchingUpPartitions = new HashSet<Key> { "P", "Q", "R" } });
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys("P", "Q", "R"));
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(Catch<IImmutableList<JobState>>.Success(
        [
            new JobState { Status = JobStatus.Running, Request = new CatchUpObserverPartitionRequest(_request.ObserverKey, ObserverType.Projection, "Q", 10UL, []) },
            new JobState { Status = JobStatus.Stopped, Request = new CatchUpObserverPartitionRequest(_request.ObserverKey, ObserverType.Projection, "R", 10UL, []) }
        ])));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_exclude_only_the_live_job_owned_partition() => ((HandleEventsForObserverArguments)_job.PreparedSteps!.Single().Request).ExcludedPartitions.ShouldContainOnly((Key)"Q");
    [Fact] void should_cover_the_ownerless_partition_with_the_walk() => _observer.Received(1)
        .RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.Contains((Key)"P")));
    [Fact] void should_cover_the_stopped_job_partition_with_the_walk() => _observer.Received(1)
        .RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.Contains((Key)"R")));
}

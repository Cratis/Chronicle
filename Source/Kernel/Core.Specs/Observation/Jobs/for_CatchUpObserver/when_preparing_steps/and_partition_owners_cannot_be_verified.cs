// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_partition_owners_cannot_be_verified : given.a_catch_up_observer_job
{
    void Establish()
    {
        _observer.GetState().Returns(new ObserverState { CatchingUpPartitions = new HashSet<Key> { "P" } });
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys("P"));
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(Catch<IImmutableList<JobState>>.Failed(new InvalidOperationException("Job storage is unavailable"))));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_prepare_the_walk_without_an_ownership_lookup() => _job.PreparedSteps!.Count.ShouldEqual(1);
    [Fact] void should_cover_the_partition_without_an_exclusion() => _observer.Received(1)
        .RegisterCatchingUpPartitions(Arg.Is<IEnumerable<Key>>(_ => _.Contains((Key)"P")));
}

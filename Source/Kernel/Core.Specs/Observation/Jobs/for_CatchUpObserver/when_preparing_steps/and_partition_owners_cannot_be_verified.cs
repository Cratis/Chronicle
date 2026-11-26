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

    [Fact] void should_not_prepare_an_overlapping_walk() => _job.PreparedSteps.ShouldBeNull();
    [Fact] void should_not_register_unverified_partition_holds() => _observer.DidNotReceive()
        .RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>());
}

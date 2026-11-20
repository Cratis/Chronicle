// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

public class when_a_partition_is_catching_up : given.a_subscribed_client_observer
{
    static readonly Key _partition = new("held-partition", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, [])
        });
    }

    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod * 2);
    }

    [Fact] void should_keep_the_activation() => ReleaseRequests.ShouldEqual(0);
}

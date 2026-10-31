// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Recovery after subscribing starts a job for a partition that was in flight and routing then clears the markers that
/// said so. The partition is still owned by that job, so another partition's retry moving the position past its
/// queued event is not suspected.
/// </summary>
public class and_a_partition_was_in_flight_when_the_subscription_is_recovered : given.an_observer_with_skip_detection
{
    async Task Establish()
    {
        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 102UL,
            InFlightPartitions = new HashSet<Key> { "Q" }
        };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero);
        await _observer.FailedPartitionRecovered("P", 103UL);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);
    }

    [Fact] void should_start_a_job_for_the_in_flight_partition() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"Q"));

    [Fact] void should_not_measure_a_suspected_skip() => _metrics.For(EventsSuspectedSkipped).ShouldBeEmpty();
}

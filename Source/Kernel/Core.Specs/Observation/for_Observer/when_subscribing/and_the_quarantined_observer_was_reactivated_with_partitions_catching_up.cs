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
/// Leaving the quarantine must not forget which in-flight partitions were already catching up, or subscribing would
/// start a second catch-up for each of them next to the one that resumes.
/// </summary>
public class and_the_quarantined_observer_was_reactivated_with_partitions_catching_up : given.a_reactivated_quarantined_observer
{
    Key _catchingUpPartition;
    Key _inFlightPartition;

    void Establish()
    {
        _catchingUpPartition = "partition-catching-up";
        _inFlightPartition = "partition-in-flight";
        _stateStorage.State = _stateStorage.State with
        {
            InFlightPartitions = new HashSet<Key> { _catchingUpPartition, _inFlightPartition },
            CatchingUpPartitions = new HashSet<Key> { _catchingUpPartition }
        };

        _jobsManager
            .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();

    [Fact]
    void should_catch_up_the_in_flight_partition() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _inFlightPartition));

    [Fact]
    void should_not_start_another_catch_up_for_the_partition_already_catching_up() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _catchingUpPartition));
}

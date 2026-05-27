// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up_in_flight_partitions;

public class and_there_are_no_pending_entries : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish() => _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>()).Returns([]);

    async Task Because() => await _observer.CatchUpInFlightPartitions();

    [Fact] void should_not_start_any_catch_up_jobs() => _jobsManager
        .DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
}

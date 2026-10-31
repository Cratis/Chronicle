// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_catch_up_job_start_is_refused : given.an_observer_with_a_live_delivery_batch
{
    void Establish() => _jobsManager
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
        .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));

    async Task Because() => await _observer.Handle(Partition, _events);

    [Fact] void should_deliver_the_batch_live() => _subscriber.Received(1).OnNext(Partition, Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == BatchSize), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_leave_the_partition_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(Partition);
}

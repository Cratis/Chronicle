// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_an_isolated_reducer_batch_fails : given.a_performing_job_step
{
    void Establish()
    {
        _performState.ReducerReplayJobId = Guid.NewGuid();
        var replay = Substitute.For<IReducerReplay>();
        replay.Begin(_performState.ReducerReplayJobId).Returns(new ReplayContext(new("model", 1), "Model", "Revert", DateTimeOffset.UtcNow) { ReplayContainerName = "isolated" });
        _silo.AddProbe(_ => replay);
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "rebuild failed"));
    }
    async Task Because() => await _jobStep.InvokePerformStep(_performState);
    [Fact] void should_stop_at_the_failed_batch() => _observerSubscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_schedule_a_live_partition_retry_from_unpublished_work() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}

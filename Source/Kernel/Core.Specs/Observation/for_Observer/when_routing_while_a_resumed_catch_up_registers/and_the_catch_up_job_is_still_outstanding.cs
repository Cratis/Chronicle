// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_while_a_resumed_catch_up_registers;

/// <summary>
/// Routing keeps the exact holds of a live observer-wide catch-up job, so the blanket hold can come down without a delivery window (#5406).
/// </summary>
public class and_the_catch_up_job_is_still_outstanding : given.an_observer_with_a_gated_state_write
{
    static readonly JobId _jobId = JobId.New();
    bool _isPreparingCatchup;

    void Establish()
    {
        HasOutstandingJobs(new JobState { Id = _jobId, Status = JobStatus.Running, Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []) });
        _gatedStateStorage.PersistedState = _gatedStateStorage.State with { NextEventSequenceNumber = 40UL };
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(44UL));
    }

    async Task Because()
    {
        var generation = await _observer.ResumingCatchUp(_jobId);
        _gatedStateStorage.SuspendNextWrite = true;
        var registration = _observer.RegisterResumedCatchingUpPartitions(_jobId, generation, [(Key)"P"]);
        await given.bounded_barrier.Finishes(_gatedStateStorage.WriteStarted.Task);
        var routing = _observer.TransitionTo<Routing>();
        await _routingLogger.WaitUntilReconciled();
        _gatedStateStorage.ReleaseWrite.SetResult();
        await given.bounded_barrier.Finishes(registration);
        await given.bounded_barrier.Finishes(routing);
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);
    }

    [Fact] void should_still_hold_the_partition() => _gatedStateStorage.State.CatchingUpPartitions.ShouldContain((Key)"P");
    [Fact] void should_lower_the_preparing_flag() => _isPreparingCatchup.ShouldBeFalse();
    [Fact] void should_not_deliver_the_held_partition() => _subscriber.DidNotReceive().OnNext((Key)"P", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}

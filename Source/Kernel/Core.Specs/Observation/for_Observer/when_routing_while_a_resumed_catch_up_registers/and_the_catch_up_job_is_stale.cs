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
/// Routing releases the holds of a stale job, so the registration must not lower the blanket hold over holds that are gone (#5406).
/// </summary>
public class and_the_catch_up_job_is_stale : given.an_observer_with_a_gated_state_write
{
    static readonly JobId _jobId = JobId.New();
    bool _isPreparingCatchup;

    void Establish()
    {
        HasOutstandingJobs(new JobState { Id = _jobId, Status = JobStatus.Running, Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []), Progress = new() { TotalSteps = 3, SuccessfulSteps = 3 }, StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow - JobLiveness.StaleAfter - TimeSpan.FromSeconds(1) }] });
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
        await _gatedStateStorage.WriteStarted.Task;
        var jobListings = _jobStorage.ReceivedCalls().Count();
        var routing = _observer.TransitionTo<Routing>();
        for (var wait = 0; wait < 300 && _jobStorage.ReceivedCalls().Count() == jobListings; wait++) await Task.Delay(10);
        await Task.Delay(100);
        _gatedStateStorage.ReleaseWrite.SetResult();
        await registration;
        await routing;
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);
    }

    [Fact] void should_have_released_the_partition() => _gatedStateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
    [Fact] void should_keep_the_blanket_hold() => _isPreparingCatchup.ShouldBeTrue();
    [Fact] void should_not_deliver_the_partition() => _subscriber.DidNotReceive().OnNext((Key)"P", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}

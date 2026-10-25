// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_routing_while_a_resumed_catch_up_registers;

/// <summary>
/// A job that concludes while its registration is being persisted owns the flag through its handover, not through the registration (#5406).
/// </summary>
public class and_the_job_concludes_during_the_write : given.an_observer_with_a_gated_state_write
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
        await _gatedStateStorage.WriteStarted.Task;
        var concluding = _observer.CaughtUp(_jobId, 41UL);
        await Task.Delay(100);
        _gatedStateStorage.ReleaseWrite.SetResult();
        await registration;
        await concluding;
        _isPreparingCatchup = await _observer.IsPreparingCatchup();
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL)]);
    }

    [Fact] void should_not_leave_the_partition_held() => _gatedStateStorage.State.CatchingUpPartitions.ShouldNotContain((Key)"P");
    [Fact] void should_not_leave_the_catch_up_preparing() => _isPreparingCatchup.ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_catch_up_repeatedly_finds_no_keys : given.an_observer_with_zero_key_catch_up_jobs
{
    const int watchdog_passes = 5;
    int _jobsBeforeWatchdog;
    int _warningsBeforeWatchdog;

    async Task Establish()
    {
        // Seed a stranded preparation so the watchdog's rescue, not a direct catch-up call, starts the real jobs.
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
        await _observer.CatchUp();
        (await _observer.IsPreparingCatchup()).ShouldBeTrue();
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(call => StartZeroKeyJob(call.Arg<CatchUpObserverRequest>()));
        await _observer.RunWatchdogAsync();
        await CompletePendingJobs();
        await CompletePendingJobs();
        _jobsBeforeWatchdog = _jobsManager.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_jobsManager.Start));
        _warningsBeforeWatchdog = NoStepsWarnings;
        _preparedJobs.Count.ShouldEqual(2);
        _warningsBeforeWatchdog.ShouldEqual(2);
        _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_nextSequenceNumber);
        (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    }

    async Task Because()
    {
        for (var pass = 0; pass < watchdog_passes; pass++)
        {
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
        }

        // Live delivery must still work after settling; a logical subscription alone would hide a stuck observer.
        if (_queued)
        {
            await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _nextSequenceNumber)]);
        }
    }

    [Fact]
    async Task should_not_create_a_zero_step_job_on_every_watchdog_pass() =>
        await _jobsManager.Received(_jobsBeforeWatchdog).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());

    [Fact] void should_not_repeat_the_no_steps_warning() => NoStepsWarnings.ShouldEqual(_warningsBeforeWatchdog);
    [Fact] async Task should_keep_the_logical_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_keep_the_physical_subscription() => _queued.ShouldBeTrue();
    [Fact] async Task should_settle_in_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_not_remain_preparing_catch_up() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] void should_rescue_the_initial_stranded_preparation_only_once() => _observer.CatchupRecoveryAttempts.ShouldEqual(1);
    [Fact] async Task should_deliver_a_live_event() =>
        await _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());

    int NoStepsWarnings => _recordedLogs.Warnings.Count(_ => _.Contains("NoJobStepsToStart", StringComparison.Ordinal));
}

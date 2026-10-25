// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_the_queue_is_lost_while_zero_key_catch_up_settles : given.an_observer_with_zero_key_catch_up_jobs
{
    const int Passes = 5;
    readonly List<int> _jobsAtLostSettle = [];
    readonly List<int> _warningsAtLostSettle = [];
    readonly List<int> _jobsAfterRestoration = [];
    readonly List<int> _warningsAfterRestoration = [];
    readonly List<int> _jobsAfterWatchdog = [];
    readonly List<bool> _backoffsAtLostSettle = [];
    readonly List<bool> _restoredSubscriptions = [];

    async Task Establish()
    {
        await SettleAfterZeroKeyJobs();
        _preparedJobs.Count.ShouldEqual(2);
        NoStepsWarnings.ShouldEqual(2);
        (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    }

    async Task Because()
    {
        for (var pass = 0; pass < Passes; pass++)
        {
            await LoseQueueDuringSettle();
            await CompletePendingJobs();
            _jobsAfterRestoration.Add(_preparedJobs.Count);
            _warningsAfterRestoration.Add(NoStepsWarnings);
            _restoredSubscriptions.Add(_queued);
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
            _jobsAfterWatchdog.Add(_preparedJobs.Count);
        }

        if (_queued)
        {
            await _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _nextSequenceNumber)]);
        }
    }

    [Fact] void should_bound_zero_key_jobs_despite_repeated_queue_loss() =>
        _jobsAtLostSettle.ShouldEqual(Enumerable.Repeat(3, Passes));

    [Fact] void should_bound_no_steps_warnings_despite_repeated_queue_loss() =>
        _warningsAtLostSettle.ShouldEqual(Enumerable.Repeat(3, Passes));

    [Fact] void should_not_start_successor_jobs_while_restoring_the_queue_inside_backoff() =>
        _jobsAfterRestoration.ShouldEqual(Enumerable.Repeat(3, Passes));

    [Fact] void should_not_warn_again_while_restoring_the_queue_inside_backoff() =>
        _warningsAfterRestoration.ShouldEqual(Enumerable.Repeat(3, Passes));

    [Fact] void should_not_restart_zero_key_jobs_on_subsequent_watchdog_passes() =>
        _jobsAfterWatchdog.ShouldEqual(Enumerable.Repeat(3, Passes));

    [Fact] void should_preserve_backoff_when_settling_without_a_queue_subscription() =>
        _backoffsAtLostSettle.ShouldEqual(Enumerable.Repeat(true, Passes));

    [Fact] void should_restore_the_queue_subscription_on_every_pass() =>
        _restoredSubscriptions.ShouldEqual(Enumerable.Repeat(true, Passes));

    [Fact] void should_not_advance_the_clock() => _clock.GetUtcNow().ShouldEqual(DateTimeOffset.UnixEpoch);
    [Fact] async Task should_keep_the_logical_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_settle_in_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_not_remain_preparing_catch_up() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
    [Fact] async Task should_deliver_a_live_event_after_restoring_the_queue() =>
        await _subscriber.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());

    async Task LoseQueueDuringSettle()
    {
        _queued = false;
        await _observer.RunWatchdogAsync();

        // Recovery restores the queue before the job finishes; lose it again before CaughtUp settles.
        _queued = false;
        await CompletePendingJobs();
        _jobsAtLostSettle.Add(_preparedJobs.Count);
        _warningsAtLostSettle.Add(NoStepsWarnings);
        _backoffsAtLostSettle.Add(await _observer.IsCatchUpBackedOff());
    }
}

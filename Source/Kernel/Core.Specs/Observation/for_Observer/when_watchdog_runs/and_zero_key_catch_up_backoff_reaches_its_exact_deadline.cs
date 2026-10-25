// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_zero_key_catch_up_backoff_reaches_its_exact_deadline : given.an_observer_with_zero_key_catch_up_jobs
{
    const int WatchdogPasses = 5;
    readonly List<int> _jobsBeforeDeadline = [];
    readonly List<int> _warningsBeforeDeadline = [];
    readonly List<int> _jobsAtDeadline = [];
    readonly List<int> _warningsAtDeadline = [];
    bool _backedOffBeforeDeadline;

    async Task Establish()
    {
        await SettleAfterZeroKeyJobs();
        _preparedJobs.Count.ShouldEqual(2);
        NoStepsWarnings.ShouldEqual(2);
        (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(5) - TimeSpan.FromTicks(1));
        _backedOffBeforeDeadline = await _observer.IsCatchUpBackedOff();
        for (var pass = 0; pass < WatchdogPasses; pass++)
        {
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
            _jobsBeforeDeadline.Add(_preparedJobs.Count);
            _warningsBeforeDeadline.Add(NoStepsWarnings);
        }

        _clock.Advance(TimeSpan.FromTicks(1));
        for (var pass = 0; pass < WatchdogPasses; pass++)
        {
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
            _jobsAtDeadline.Add(_preparedJobs.Count);
            _warningsAtDeadline.Add(NoStepsWarnings);
        }
    }

    [Fact] void should_remain_backed_off_one_tick_before_the_deadline() => _backedOffBeforeDeadline.ShouldBeTrue();
    [Fact] void should_not_restart_jobs_before_the_deadline() => _jobsBeforeDeadline.ShouldEqual(Enumerable.Repeat(2, WatchdogPasses));
    [Fact] void should_not_warn_before_the_deadline() => _warningsBeforeDeadline.ShouldEqual(Enumerable.Repeat(2, WatchdogPasses));
    [Fact] void should_restart_only_once_at_the_exact_deadline() => _jobsAtDeadline.ShouldEqual(Enumerable.Repeat(3, WatchdogPasses));
    [Fact] void should_warn_only_once_at_the_exact_deadline() => _warningsAtDeadline.ShouldEqual(Enumerable.Repeat(3, WatchdogPasses));
    [Fact] void should_keep_the_queue_subscribed() => _queued.ShouldBeTrue();
    [Fact] async Task should_back_off_again_after_the_expired_lap() => (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
}

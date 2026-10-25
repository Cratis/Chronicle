// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_zero_key_catch_up_backoff_expires_repeatedly : given.an_observer_with_zero_key_catch_up_jobs
{
    const int WatchdogPassesInsideWindow = 5;
    readonly List<int> _jobsAtExpiry = [];
    readonly List<int> _warningsAtExpiry = [];
    readonly List<int> _jobsInsideWindows = [];
    readonly List<int> _warningsInsideWindows = [];
    readonly List<bool> _insideBackoffs = [];

    async Task Establish()
    {
        await SettleAfterZeroKeyJobs();
        _preparedJobs.Count.ShouldEqual(2);
        NoStepsWarnings.ShouldEqual(2);
        (await _observer.IsCatchUpBackedOff()).ShouldBeTrue();
        RecordExpiryCounts();
    }

    async Task Because()
    {
        // Consecutive zero-work laps grow the delay from 5 seconds to the 5-minute cap, without wall-clock waits.
        foreach (var seconds in new[] { 5, 10, 20, 40, 80, 160, 300, 300 })
        {
            _clock.Advance(TimeSpan.FromSeconds(seconds) - TimeSpan.FromTicks(1));
            await RunWatchdogInsideWindow();
            _clock.Advance(TimeSpan.FromTicks(2));
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
            await CompletePendingJobs();
            RecordExpiryCounts();
        }
    }

    [Fact] void should_start_only_one_zero_key_job_per_growing_backoff_expiry() =>
        _jobsAtExpiry.ShouldEqual([2, 3, 4, 5, 6, 7, 8, 9, 10]);

    [Fact] void should_warn_only_once_per_growing_backoff_expiry() =>
        _warningsAtExpiry.ShouldEqual([2, 3, 4, 5, 6, 7, 8, 9, 10]);

    [Fact] void should_not_start_jobs_inside_the_growing_backoff_windows() =>
        _jobsInsideWindows.ShouldEqual([2, 3, 4, 5, 6, 7, 8, 9]);

    [Fact] void should_not_warn_inside_the_growing_backoff_windows() =>
        _warningsInsideWindows.ShouldEqual([2, 3, 4, 5, 6, 7, 8, 9]);

    [Fact] void should_remain_backed_off_until_each_expected_expiry() =>
        _insideBackoffs.ShouldEqual(Enumerable.Repeat(true, 8));

    [Fact] void should_not_reset_the_consecutive_zero_work_lap_count() =>
        _recordedLogs.Warnings.Where(_ => _.StartsWith("A catch-up job concluded without moving", StringComparison.Ordinal))
            .ShouldEqual(Enumerable.Range(1, 10).Select(lap => $"A catch-up job concluded without moving the observer forward ({lap} in a row)"));

    async Task RunWatchdogInsideWindow()
    {
        _insideBackoffs.Add(await _observer.IsCatchUpBackedOff());
        for (var pass = 0; pass < WatchdogPassesInsideWindow; pass++)
        {
            await _observer.RunWatchdogAsync();
            await CompletePendingJobs();
        }

        _jobsInsideWindows.Add(_preparedJobs.Count);
        _warningsInsideWindows.Add(NoStepsWarnings);
    }

    void RecordExpiryCounts()
    {
        _jobsAtExpiry.Add(_preparedJobs.Count);
        _warningsAtExpiry.Add(NoStepsWarnings);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_last_seen_at;

public class when_the_watchdog_runs_while_a_client_holds_the_subscription : given.a_client_observer
{
    int _savesBeforeTheIntervalPassed;

    async Task Establish() => await SubscribeClient();

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromMinutes(10));
        await _observer.RunWatchdogAsync();
        _savesBeforeTheIntervalPassed = LastSeenSaves;

        _clock.Advance(TimeSpan.FromHours(1));
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_write_again_within_the_refresh_interval() => _savesBeforeTheIntervalPassed.ShouldEqual(1);
    [Fact] void should_refresh_the_time_once_the_interval_has_passed() => LastSeenSaves.ShouldEqual(2);
}

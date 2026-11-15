// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

public class and_the_stream_closes : given.a_client_subscription_keeper
{
    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        await _clock.NextWait();
        await _streamClosed.CancelAsync();
    }

    [Fact] async Task should_complete() => await Observation.given.ManualClock.Within(_keeping);
    [Fact] void should_have_checked_once() => _checks.ShouldEqual(1);
}

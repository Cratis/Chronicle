// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

public class and_the_stream_closes : given.a_client_subscription_keeper
{
    async Task Because()
    {
        await StartKeeping();
        await _periodicCheck();
        await _streamClosed.CancelAsync();
        await _keeping.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_have_checked_once() => _checks.ShouldEqual(1);
    [Fact] void should_leave_the_supervisor() => _unregistered.ShouldEqual(1);
    [Fact] void should_unregister_the_key_it_registered() => _supervisor.Received(1).Unregister(_key);
}

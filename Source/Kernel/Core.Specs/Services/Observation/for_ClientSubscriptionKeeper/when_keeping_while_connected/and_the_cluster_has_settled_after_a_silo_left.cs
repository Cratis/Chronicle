// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// Once the settle window after a departure has passed, an intact subscription is left to the safety net again.
/// </summary>
public class and_the_cluster_has_settled_after_a_silo_left : given.a_client_subscription_keeper
{
    TimeSpan _waitOnceSettled;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        await _clock.NextWait();
        SiloLeaves();
        await ElapseAtLeast(Settle);
        _waitOnceSettled = await _clock.NextWait();
    }

    [Fact] void should_leave_it_to_the_safety_net() => _waitOnceSettled.ShouldEqual(SafetyNetWait);
}

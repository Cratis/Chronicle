// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// The safety net is what restores a subscription lost without a silo leaving: an intact subscription is checked again
/// once the safety-net wait has passed, never later than the safety-net interval.
/// </summary>
public class and_the_safety_net_elapses : given.a_client_subscription_keeper
{
    TimeSpan _safetyNetWait;
    TimeSpan _waitAfterTheSecondCheck;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        _safetyNetWait = await _clock.Elapse();
        _waitAfterTheSecondCheck = await _clock.NextWait();
    }

    [Fact] void should_check_again() => _checks.ShouldEqual(2);
    [Fact] void should_wait_the_jittered_safety_net() => _safetyNetWait.ShouldEqual(SafetyNetWait);
    [Fact] void should_wait_no_longer_than_the_safety_net_interval() => (_safetyNetWait <= SafetyNet).ShouldBeTrue();
    [Fact] void should_go_back_to_the_safety_net() => _waitAfterTheSecondCheck.ShouldEqual(SafetyNetWait);
}

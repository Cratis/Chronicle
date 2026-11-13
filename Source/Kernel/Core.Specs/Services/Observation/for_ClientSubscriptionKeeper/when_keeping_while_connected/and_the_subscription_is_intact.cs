// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A subscription found intact is not checked again until the safety-net wait has passed.
/// </summary>
public class and_the_subscription_is_intact : given.a_client_subscription_keeper
{
    TimeSpan _firstWait;
    TimeSpan _waitAfterTheCheck;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        _firstWait = await _clock.Elapse();
        _waitAfterTheCheck = await _clock.NextWait();
    }

    [Fact] void should_check_after_one_revise_interval() => _firstWait.ShouldEqual(Revise);
    [Fact] void should_check_once() => _checks.ShouldEqual(1);
    [Fact] void should_wait_for_the_safety_net() => _waitAfterTheCheck.ShouldEqual(SafetyNetWait);
}

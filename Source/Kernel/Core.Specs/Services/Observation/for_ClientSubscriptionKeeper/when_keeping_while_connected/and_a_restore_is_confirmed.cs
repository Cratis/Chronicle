// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A restored subscription is checked again after one revise interval to confirm it, and then left to the safety net.
/// </summary>
public class and_a_restore_is_confirmed : given.a_client_subscription_keeper
{
    TimeSpan _waitToConfirm;
    TimeSpan _waitAfterConfirming;

    protected override Task<bool> Check(int check) => Task.FromResult(check > 1);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        _waitToConfirm = await _clock.Elapse();
        _waitAfterConfirming = await _clock.NextWait();
    }

    [Fact] void should_confirm_after_one_revise_interval() => _waitToConfirm.ShouldEqual(Revise);
    [Fact] void should_check_until_the_restore_is_confirmed() => _checks.ShouldEqual(2);
    [Fact] void should_leave_it_to_the_safety_net() => _waitAfterConfirming.ShouldEqual(SafetyNetWait);
}

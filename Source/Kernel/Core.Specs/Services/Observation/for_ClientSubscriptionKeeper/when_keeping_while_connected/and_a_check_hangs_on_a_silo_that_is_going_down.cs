// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A check that asks an observer on a silo that is going down hangs until the response timeout. It must not keep the
/// subscription unchecked for that long: it is given up as failed, so a departure asks again and restores the subscription.
/// </summary>
public class and_a_check_hangs_on_a_silo_that_is_going_down : given.a_client_subscription_keeper
{
    protected override TimeSpan? CheckTimeout => TimeSpan.FromMilliseconds(50);

    TimeSpan? _waitAfterTheHang;

    protected override Task<bool> Check(int check) =>
        check == 1 ? new TaskCompletionSource<bool>().Task : Task.FromResult(true);

    async Task Because()
    {
        await StartKeeping();
        _waitAfterTheHang = await _periodicCheck();
        await _checkAfterDeparture();
    }

    [Fact] void should_give_up_the_hung_check_and_check_again_after_one_revise_interval() => _waitAfterTheHang.ShouldEqual(Revise);
    [Fact] void should_check_again_when_a_silo_leaves() => _checks.ShouldEqual(2);
}

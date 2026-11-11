// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// In a rolling deployment silos leave one after the other. Each departure starts a full settle window, so the checks
/// continue every revise interval past the end of the first one.
/// </summary>
public class and_a_second_silo_leaves_while_settling : given.a_client_subscription_keeper
{
    static readonly TimeSpan _intoTheFirstWindow = Settle * 2 / 3;

    TimeSpan _waitAfterTheSecondDeparture;
    List<TimeSpan> _waitsWhileSettlingAgain;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        await _clock.NextWait();
        SiloLeaves();
        await ElapseAtLeast(_intoTheFirstWindow);
        await _clock.NextWait();
        SiloLeaves();
        _waitAfterTheSecondDeparture = await _clock.Elapse();
        _waitsWhileSettlingAgain = await ElapseAtLeast(Settle - Spread - Revise);
        await _clock.NextWait();
    }

    [Fact] void should_spread_a_check_after_the_second_departure() => _waitAfterTheSecondDeparture.ShouldEqual(Spread);
    [Fact] void should_check_every_revise_interval_past_the_first_window() => _waitsWhileSettlingAgain.TrueForAll(wait => wait == Revise).ShouldBeTrue();
    [Fact] void should_have_reached_past_the_first_window() => (_intoTheFirstWindow + (Revise * _waitsWhileSettlingAgain.Count) > Settle).ShouldBeTrue();
}

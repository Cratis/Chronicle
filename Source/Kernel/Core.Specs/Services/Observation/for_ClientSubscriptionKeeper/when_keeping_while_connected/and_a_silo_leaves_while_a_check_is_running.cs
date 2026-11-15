// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A check against an observer on a dying silo can hang until the call times out, which is exactly when silos leave. A
/// departure signalled while a check runs must still start the settle window once the check is done.
/// </summary>
public class and_a_silo_leaves_while_a_check_is_running : given.a_client_subscription_keeper
{
    readonly TaskCompletionSource _checking = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _checked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TimeSpan _waitAfterTheCheck;

    protected override Task<bool> Check(int check)
    {
        if (check > 1)
        {
            return Task.FromResult(true);
        }

        _checking.SetResult();
        return _checked.Task;
    }

    async Task Because()
    {
        StartKeeping();
        await _clock.Elapse();
        await Observation.given.ManualClock.Within(_checking.Task);
        SiloLeaves();
        _checked.SetResult(true);
        _waitAfterTheCheck = await _clock.NextWait();
    }

    [Fact] void should_spread_a_check_after_the_departure() => _waitAfterTheCheck.ShouldEqual(Spread);
}

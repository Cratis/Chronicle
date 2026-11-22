// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A check against an observer on a dying silo can hang until the call times out, which is exactly when silos leave. The
/// check in flight already covers a departure signalled meanwhile, so no second check is started over it.
/// </summary>
public class and_a_silo_leaves_while_a_check_is_running : given.a_client_subscription_keeper
{
    readonly TaskCompletionSource _checking = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _checked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected override Task<bool> Check(int check)
    {
        _checking.TrySetResult();
        return _checked.Task;
    }

    async Task Because()
    {
        await StartKeeping();
        var running = _periodicCheck();
        await _checking.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _checkAfterDeparture();
        _checked.SetResult(true);
        await running;
    }

    [Fact] void should_not_check_a_second_time_over_the_running_one() => _checks.ShouldEqual(1);
}

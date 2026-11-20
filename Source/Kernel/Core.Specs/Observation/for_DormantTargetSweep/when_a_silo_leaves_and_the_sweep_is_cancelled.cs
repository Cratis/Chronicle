// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_a_silo_leaves_and_the_sweep_is_cancelled : given.a_dormant_target_sweep
{
    readonly TaskCompletionSource _called = new();
    readonly TaskCompletionSource _neverCompletes = new();
    CancellationTokenSource _cancellation;
    Task<int> _sweeping;
    bool _returned;

    void Establish()
    {
        _heldObserver.RunWatchdogOnce().Returns(_ =>
        {
            _called.TrySetResult();
            return _neverCompletes.Task;
        });
        _cancellation = new();
    }

    async Task Because()
    {
        _sweeping = _sweep.SilosLeft([LeavingSilo], _cancellation.Token);
        await _called.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _cancellation.CancelAsync();
        try
        {
            await _sweeping.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            _returned = true;
        }
        catch (TimeoutException)
        {
            _returned = false;
        }
    }

    [Fact] void should_return_instead_of_waiting_for_the_observer() => _returned.ShouldBeTrue();
    [Fact] void should_give_the_wake_place_back() => _wakeAdmission.InProgress.ShouldEqual(0);
}

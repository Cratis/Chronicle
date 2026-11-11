// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// Once the settle window after a departure has passed, an intact subscription is left to the safety net again.
/// </summary>
public class and_the_cluster_has_settled_after_a_silo_left : given.a_client_subscription_keeper
{
    int _checksWhenSettled;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    protected override TimeSpan Settle => Revise * 5;

    protected override bool CloseTheStreamAt(int check) => false;

    async Task Because()
    {
        var keeping = Keep();
        await WaitForChecks(1);
        var left = _siloLeft;
        _siloLeft = new(TaskCreationOptions.RunContinuationsAsynchronously);
        left.SetResult();
        await WaitForChecks(2);
        await Task.Delay(Settle + (Revise * 10));
        _checksWhenSettled = Volatile.Read(ref _checks);
        await Task.Delay(Revise * 20);
        await _streamClosed.CancelAsync();
        await keeping.WaitAsync(Patience);
    }

    [Fact] void should_check_after_the_departure() => _checksWhenSettled.ShouldBeGreaterThan(1);
    [Fact] void should_stop_checking_every_revise_interval() => _checks.ShouldEqual(_checksWhenSettled);
}

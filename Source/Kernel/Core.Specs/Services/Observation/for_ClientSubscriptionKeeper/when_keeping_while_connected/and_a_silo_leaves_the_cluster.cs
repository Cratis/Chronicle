// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A silo leaving the cluster is what loses subscriptions, so an intact subscription is checked again without waiting
/// for the safety net, and every revise interval while the cluster settles.
/// </summary>
public class and_a_silo_leaves_the_cluster : given.a_client_subscription_keeper
{
    const int ChecksWhileSettling = 3;

    protected override Task<bool> Check(int check) => Task.FromResult(true);

    protected override bool CloseTheStreamAt(int check) => check == 1 + ChecksWhileSettling;

    async Task Because()
    {
        var keeping = Keep();
        await WaitForChecks(1);
        var left = _siloLeft;
        _siloLeft = new(TaskCreationOptions.RunContinuationsAsynchronously);
        left.SetResult();
        try
        {
            await keeping.WaitAsync(Patience);
            _completed = true;
        }
        catch (TimeoutException)
        {
            _completed = false;
        }
    }

    [Fact] void should_check_every_revise_interval_while_settling() => _completed.ShouldBeTrue();
    [Fact] void should_check_after_the_departure() => _checks.ShouldEqual(1 + ChecksWhileSettling);
}

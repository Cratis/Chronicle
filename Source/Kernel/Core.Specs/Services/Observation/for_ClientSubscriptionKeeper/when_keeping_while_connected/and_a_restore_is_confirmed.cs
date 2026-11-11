// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A restored subscription is checked again after one revise interval to confirm it, and then left to the safety net.
/// </summary>
public class and_a_restore_is_confirmed : given.a_client_subscription_keeper
{
    protected override Task<bool> Check(int check) => Task.FromResult(check > 1);

    protected override bool CloseTheStreamAt(int check) => false;

    async Task Because()
    {
        var keeping = Keep();
        await WaitForChecks(2);
        await Task.Delay(Revise * 20);
        await _streamClosed.CancelAsync();
        await keeping.WaitAsync(Patience);
    }

    [Fact] void should_check_until_the_restore_is_confirmed() => _checks.ShouldEqual(2);
}

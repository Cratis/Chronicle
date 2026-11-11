// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_keeping_while_connected;

/// <summary>
/// A subscription found intact is not checked again until the safety-net interval, however many revise intervals pass.
/// </summary>
public class and_the_subscription_is_intact : given.a_client_subscription_keeper
{
    protected override Task<bool> Check(int check) => Task.FromResult(true);

    protected override bool CloseTheStreamAt(int check) => false;

    async Task Because()
    {
        var keeping = Keep();
        await WaitForChecks(1);
        await Task.Delay(Revise * 20);
        await _streamClosed.CancelAsync();
        await keeping.WaitAsync(Patience);
    }

    [Fact] void should_check_once() => _checks.ShouldEqual(1);
}

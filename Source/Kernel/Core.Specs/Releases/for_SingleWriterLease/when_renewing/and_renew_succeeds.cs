// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_renewing;

public class and_renew_succeeds : given.a_keeper
{
    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        var renewed = Time.WhenNextTimer();
        Time.Advance(TimeSpan.FromSeconds(10));
        await renewed;
        Time.Advance(TimeSpan.FromSeconds(6));
    }

    [Fact] void should_extend_the_safety_deadline() => Keeper.IsHeld.ShouldBeTrue();
    [Fact] void should_renew_for_the_same_owner() => Storage.Received(1).Renew(Keeper.Owner, TimeSpan.FromSeconds(30), Arg.Any<CancellationToken>());
    [Fact] void should_not_stop_the_host() => Lifetime.DidNotReceive().StopApplication();
}

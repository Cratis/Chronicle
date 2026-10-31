// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_stopping;

public class and_shutdown_is_graceful : given.a_keeper
{
    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        await Observer.OnStop(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_release_the_owner_lease() => Storage.Received(1).Release(Keeper.Owner, Arg.Any<CancellationToken>());
    [Fact] void should_cancel_the_watchdog() => FailFast.DidNotReceive().FailFast(Arg.Any<string>());
    [Fact] void should_clear_readiness() => Keeper.IsHeld.ShouldBeFalse();
}

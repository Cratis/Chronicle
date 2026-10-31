// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_stopping;

public class and_the_stop_token_is_cancelled : given.a_keeper
{
    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Observer.OnStop(cancellation.Token);
    }

    [Fact] void should_still_release_using_its_own_budget() => Storage.Received(1).Release(Keeper.Owner, Arg.Is<CancellationToken>(token => !token.IsCancellationRequested));
    [Fact] void should_not_hold_the_lease() => Keeper.IsHeld.ShouldBeFalse();
}

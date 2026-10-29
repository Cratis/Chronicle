// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_renewing;

public class and_renew_hangs_past_half_ttl : given.a_keeper
{
    void Establish()
    {
        Storage.Renew(Arg.Any<SingleWriterLeaseOwner>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<bool>().Task);
    }

    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(15));
    }

    [Fact] void should_stop_at_half_ttl() => Lifetime.Received(1).StopApplication();
    [Fact] void should_begin_the_shared_drain() => Drain.Received(1).Begin();
    [Fact] void should_clear_readiness() => Keeper.IsHeld.ShouldBeFalse();
    [Fact] void should_reserve_time_for_graceful_shutdown() => FailFast.DidNotReceive().FailFast(Arg.Any<string>());
}

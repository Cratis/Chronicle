// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_renewing;

public class and_renew_is_temporarily_unavailable : given.a_keeper
{
    void Establish()
    {
        var calls = 0;
        Storage.Renew(Arg.Any<SingleWriterLeaseOwner>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(_ =>
            ++calls == 1 ? Task.FromException<bool>(new IOException("unavailable")) : Task.FromResult(true));
    }

    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        var retry = Time.WhenNextTimer();
        Time.Advance(TimeSpan.FromSeconds(10));
        await retry;
        var renewed = Time.WhenNextTimer();
        Time.Advance(TimeSpan.FromSeconds(2));
        await renewed;
    }

    [Fact] void should_keep_the_lease() => Keeper.IsHeld.ShouldBeTrue();
    [Fact] void should_retry_before_half_ttl() => Storage.Received(2).Renew(Keeper.Owner, TimeSpan.FromSeconds(30), Arg.Any<CancellationToken>());
}

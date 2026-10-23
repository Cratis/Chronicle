// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_lease_is_held_then_released : given.a_keeper
{
    bool _wasHeld;
    void Establish()
    {
        Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(new SingleWriterLeaseAttempt(false, new(new("other"), "host", "1", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(30))), new SingleWriterLeaseAttempt(true, null));
    }

    async Task Because()
    {
        var waiting = Time.WhenNextTimer();
        var start = Observer.OnStart(CancellationToken.None);
        await waiting;
        _wasHeld = Keeper.IsHeld;
        var acquired = Time.WhenNextTimer();
        Time.Advance(TimeSpan.FromSeconds(2));
        await acquired;
        await start;
    }

    [Fact] void should_wait_before_becoming_active() => _wasHeld.ShouldBeFalse();
    [Fact] void should_acquire_after_the_holder_releases() => Keeper.IsHeld.ShouldBeTrue();
}

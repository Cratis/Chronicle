// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_acquire_timeout_elapses : given.a_keeper
{
    Exception _error;
    void Establish()
    {
        Options.Clustering.SingleWriterLease.AcquireTimeout = TimeSpan.FromSeconds(1);
        CreateKeeper();
        Storage.TryAcquire(Arg.Any<SingleWriterLeaseClaim>(), Arg.Any<CancellationToken>()).Returns(new SingleWriterLeaseAttempt(false, null));
    }

    async Task Because()
    {
        var start = Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(1));
        _error = await Catch.Exception(() => start);
    }

    [Fact] void should_fail_with_the_lease_specific_error() => _error.ShouldBeOfExactType<SingleWriterLeaseNotAcquired>();
    [Fact] void should_not_hold_a_lease() => Keeper.IsHeld.ShouldBeFalse();
}
